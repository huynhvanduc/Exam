# Giữ nguyên port 6004 khi đăng nhập — YARP reverse proxy sang Identity.Server

## 1. Vấn đề & Root cause

Hiện tại có 3 service lộ port riêng ra ngoài, không có gateway chung:

| Service | Port ngoài | Vai trò |
|---|---|---|
| `exam.webapp` | `6004` | Blazor Server, nơi user thao tác |
| `identity.server` | `5001` | IdentityServer4, phát hành token |
| `exam.api` | `5000` | Exam API |

Khi user bấm "Đăng nhập" trên `Exam.WebApp`:

1. `Exam.WebApp/Program.cs:45-76` challenge OIDC. Middleware build URL `/connect/authorize` dựa trên `Authority` **nội bộ** (`http://identity.server:8080` trong Docker, hoặc `http://localhost:5001` khi chạy local) — việc lấy discovery document/token là server-to-server, không lộ ra browser.
2. Nhưng trước khi redirect **browser**, sự kiện `OnRedirectToIdentityProvider` gọi `RewriteHost(...)` để ghi đè host/port của `IssuerAddress` thành `IdentityServer:PublicAuthority` (`http://localhost:5001`).
3. Browser bị điều hướng thật sự sang `http://localhost:5001/connect/authorize?...` → **lộ port/identity của Identity Server**, rồi mới quay lại `http://localhost:6004/signin-oidc`.

Đây là hành vi Authorization Code Flow đúng chuẩn (không phải bug), nhưng vì hệ thống chưa có gateway/reverse-proxy đứng trước nên browser thấy origin thật của IdentityServer.

**Mục tiêu:** browser chỉ nói chuyện với origin `http://localhost:6004` từ đầu đến cuối luồng login/logout. Việc forward sang IdentityServer thật diễn ra ở tầng server (YARP reverse proxy nhúng trong `Exam.WebApp`).

**Không đổi:**
- `IdentityServer:Authority` nội bộ (server-to-server: discovery, `/connect/token`, `/connect/userinfo`) — vẫn gọi thẳng `identity.server:8080` / `localhost:5001`, không qua proxy.
- `IdentityServer:IssuerUri` (`appsettings.json` của Identity.Server) — vẫn là chuỗi cố định `http://localhost:5001`, **không** phụ thuộc request. Đây là điểm quan trọng: vì Issuer là tĩnh, việc proxy đường browser-facing qua path khác **không** làm lệch `iss` claim giữa discovery document và token — tránh hẳn lớp phức tạp "dynamic issuer mismatch" thường gặp khi front proxy IdentityServer.
- Port `5001` của `identity.server` vẫn mở (client `exam_api_swaggerui` đăng nhập trực tiếp qua đó, không đi qua `Exam.WebApp`).

## 2. Thiết kế

Thêm YARP ngay trong `Exam.WebApp`, proxy **toàn bộ** `identity.server` dưới prefix `/idp/**`:

```
Browser                Exam.WebApp (6004)                 identity.server (8080, nội bộ)
  |--GET /account/login-->|
  |                       |--302 Location: /idp/connect/authorize?...  (RewriteHost mới, thêm path)
  |<--302-----------------|
  |--GET /idp/connect/authorize-->| YARP match "/idp/{**catch-all}"
  |                                | strip "/idp", set header X-Forwarded-Prefix: /idp
  |                                |------------------------------>| /connect/authorize
  |                                |<-- 302 /Account/Login?ReturnUrl=/idp/connect/authorize/callback...
  |<--302 /idp/Account/Login------|<-------------------------------|
  |--GET /idp/Account/Login------>| (proxy, path base "/idp")      |
  |<--200 HTML (base href="/idp/")|<--------------------------------|
  |--POST /idp/Account/Login----->|-------------------------------->| xác thực, set cookie idsrv
  |<--302 ReturnUrl (đã có /idp)--|<--------------------------------|
  |--GET /idp/connect/authorize/callback-->|------------------------>| phát code, redirect về
  |<--302 http://localhost:6004/signin-oidc?code=...----------------|
  |--GET /signin-oidc (route gốc, không qua /idp)------------------>| Exam.WebApp tự xử lý
```

Ba việc bắt buộc phải làm đồng bộ, thiếu 1 trong 3 sẽ vỡ:

1. **YARP** strip prefix `/idp` khi forward, nhưng gắn 1 header tuỳ biến để Identity.Server biết nó đang được phục vụ dưới path nào.
2. **Identity.Server** đọc header đó và set `HttpContext.Request.PathBase` **trước** `UseRouting()`. Nhờ đó mọi URL sinh ra bên trong (redirect `ReturnUrl`, `Url.Action`, cookie path...) tự động có tiền tố `/idp` — đây là cơ chế chuẩn của ASP.NET Core cho path-based reverse proxy, không có middleware dựng sẵn nào đọc header này nên phải tự viết (rất ngắn, ~5 dòng).
3. **`<base href>`** trong `App.razor` của Identity.Server đang cứng `"/"`. Phải đổi thành động theo `PathBase`, nếu không mọi asset tương đối (`_framework/blazor.web.js`) và `NavigationManager.BaseUri` phía client sẽ resolve sai về gốc `/` thay vì `/idp/`.

> **Lưu ý quan trọng phát hiện khi implement (không dùng tên header `X-Forwarded-Prefix`):** YARP tự động gắn sẵn header `X-Forwarded-Prefix` cho MỌI route theo mặc định — giá trị lấy từ `PathBase` **thật** của request tới `Exam.WebApp` (luôn rỗng ở đây, vì `/idp` nằm trong `Path` chứ không phải `PathBase` phía WebApp). Nếu đặt transform tuỳ biến trùng tên `X-Forwarded-Prefix`, giá trị mặc định (rỗng) sẽ **ghi đè lại** giá trị `/idp` mình set — im lặng, không lỗi, rất khó nhận ra (login vẫn redirect được, chỉ thiếu mỗi `/idp` trong URL). Giải pháp: dùng tên header riêng không đụng namespace mặc định của YARP, ví dụ `X-Proxy-Base-Path`. Xem mục 4.2/4.4 bên dưới.

**Vì sao không chọn "luôn luôn `UsePathBase("/idp")` cố định trong Identity.Server"** (đơn giản hơn, không cần đọc header): vì như vậy port `5001` truy cập trực tiếp (client `exam_api_swaggerui`, test thủ công) cũng bắt buộc phải thêm `/idp`, kéo theo phải sửa `Authority` ở cả `exam.api` lẫn `exam.webapp` cho các cuộc gọi nội bộ, và `RedirectUris` của các client khác. Cách "chỉ set PathBase khi có header từ proxy" giữ nguyên toàn bộ truy cập trực tiếp/nội bộ hiện có, chỉ ảnh hưởng đúng đường browser đi qua `Exam.WebApp`.

## 3. Checklist file thay đổi

- [ ] `src/Web/Exam.WebApp/Exam.WebApp.csproj` — thêm package `Yarp.ReverseProxy`
- [ ] `src/Web/Exam.WebApp/appsettings.json` — thêm section `ReverseProxy`, sửa `PublicAuthority`
- [ ] `src/Web/Exam.WebApp/appsettings.Development.json` — không cần sửa (đã trùng logic với `appsettings.json` khi chạy local, xem bước 5.2)
- [ ] `src/Web/Exam.WebApp/Program.cs` — đăng ký YARP, sửa `RewriteHost`
- [ ] `src/Services/Identity/Identity.Server/Program.cs` — middleware set `PathBase` từ `X-Forwarded-Prefix`
- [ ] `src/Services/Identity/Identity.Server/Components/App.razor` — `<base href>` động
- [ ] `docker-compose.yml` — sửa giá trị env `IdentityServer__PublicAuthority` của `exam.webapp`

## 4. Chi tiết từng bước

### 4.1. Thêm package YARP vào Exam.WebApp

`src/Web/Exam.WebApp/Exam.WebApp.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.*" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.*" />
  <PackageReference Include="Yarp.ReverseProxy" Version="2.*" />
</ItemGroup>
```

Cài qua CLI (tương đương):

```
dotnet add src/Web/Exam.WebApp/Exam.WebApp.csproj package Yarp.ReverseProxy
```

### 4.2. Cấu hình ReverseProxy trong appsettings

`src/Web/Exam.WebApp/appsettings.json` — thêm section mới, và **sửa** `PublicAuthority` để bao gồm prefix:

```jsonc
{
  // ... giữ nguyên Logging/AllowedHosts ...
  "IdentityServer": {
    "Authority": "http://localhost:5001",
    "PublicAuthority": "http://localhost:6004/idp"   // <-- đổi từ "http://localhost:5001"
  },
  "ExamApi": {
    "BaseUrl": "http://localhost:5000"
  },
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/idp/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/idp" },
          { "RequestHeader": "X-Proxy-Base-Path", "Set": "/idp" },
          { "RequestHeaderOriginalHost": "true" }
        ]
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": {
            // Trùng giá trị với IdentityServer:Authority — cùng trỏ vào IdentityServer nội bộ
            "Address": "http://localhost:5001/"
          }
        }
      }
    }
  }
}
```

Vì địa chỉ đích của cluster (`identity-cluster`) và `IdentityServer:Authority` luôn là cùng một host, để tránh khai báo trùng 2 nơi, đọc giá trị đó trong code thay vì hard-code trong JSON — xem bước 4.3 (`LoadFromConfig` rồi override `Destinations` bằng code là hơi rắc rối; đơn giản nhất là **giữ URL giống hệt `IdentityServer:Authority`** ở cả 2 appsettings và ở docker-compose, coi đây là 1 quy ước phải nhớ khi đổi hạ tầng — ghi rõ comment trong JSON).

`{ "RequestHeaderOriginalHost": "true" }` **bắt buộc phải có**, không phải tuỳ chọn: mặc định YARP KHÔNG forward `Host` header gốc của request, mà thay bằng host của `Destination` (`localhost:5001`/`identity.server:8080`). Nếu thiếu transform này, mọi absolute URL mà IdentityServer4 tự sinh ra (ví dụ redirect sang trang login khi chưa đăng nhập) sẽ mang host nội bộ đó thay vì `localhost:6004` — thực tế gặp phải khi test: `Location: http://identity.server:8080/Account/Login?...` lộ thẳng hostname Docker nội bộ ra browser, phản tác dụng hoàn toàn so với mục tiêu ban đầu.

`docker-compose.yml` — service `exam.webapp`, thêm biến cho `ReverseProxy` cluster destination (khớp `IdentityServer__Authority` sẵn có) và sửa `PublicAuthority`:

```yaml
  exam.webapp:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      IdentityServer__Authority: "http://identity.server:8080"
      IdentityServer__PublicAuthority: "http://localhost:6004/idp"     # <-- đổi
      ReverseProxy__Clusters__identity-cluster__Destinations__destination1__Address: "http://identity.server:8080/"  # <-- thêm
      ExamApi__BaseUrl: "http://exam.api:8080"
```

(Biến env dạng `ReverseProxy__Clusters__identity-cluster__Destinations__destination1__Address` override đúng path JSON `ReverseProxy:Clusters:identity-cluster:Destinations:destination1:Address` nhờ cơ chế cấu hình phân cấp chuẩn của .NET — không cần code thêm.)

`appsettings.Development.json` của WebApp không cần đổi gì (nó chỉ override `Logging`, không đụng tới `IdentityServer`/`ReverseProxy`) — giá trị lấy từ `appsettings.json` khi chạy local là đủ.

### 4.3. `Program.cs` của Exam.WebApp

Đăng ký YARP và map route — thêm sau các dòng `builder.Services.Add...` hiện có, trước `var app = builder.Build();`:

```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

Sau `app.UseAuthorization(); app.UseAntiforgery();`, thêm map:

```csharp
app.MapReverseProxy();
```

Sửa hàm `RewriteHost` để **thêm path prefix** thay vì chỉ đổi host/port — vì `PublicAuthority` giờ có dạng `http://localhost:6004/idp` (có `AbsolutePath` là `/idp`), còn `message.IssuerAddress` gốc do discovery document trả về có dạng `http://localhost:5001/connect/authorize` (chỉ có host/port khác, path `/connect/authorize` vẫn đúng, cần giữ):

```csharp
static void RewriteHost(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage message, string publicAuthority)
{
    var publicUri = new Uri(publicAuthority);
    var target = new Uri(message.IssuerAddress);

    var prefix = publicUri.AbsolutePath.TrimEnd('/');   // "/idp" (rỗng nếu PublicAuthority không có path)
    var path = prefix + target.AbsolutePath;             // "/idp/connect/authorize"

    message.IssuerAddress = new UriBuilder(target)
    {
        Scheme = publicUri.Scheme,
        Host = publicUri.Host,
        Port = publicUri.Port,
        Path = path
    }.Uri.ToString();
}
```

Hàm này đang được gọi cho cả `OnRedirectToIdentityProvider` (login) và `OnRedirectToIdentityProviderForSignOut` (logout) — không cần sửa gì thêm ở 2 handler đó, cả 2 tự động ăn theo prefix mới.

### 4.4. Identity.Server — middleware đọc `X-Proxy-Base-Path`

`src/Services/Identity/Identity.Server/Program.cs` — thêm **trước** `app.UseRouting();`:

```csharp
// YARP (Exam.WebApp) forward các request browser-facing dưới prefix "/idp" và strip prefix trước khi
// forward tới đây, gắn header X-Proxy-Base-Path báo cho biết prefix đó. Không dùng tên chuẩn
// "X-Forwarded-Prefix" vì đó là header YARP tự set mặc định theo PathBase THẬT của request gốc (luôn
// rỗng ở đây vì "/idp" nằm trong Path chứ không phải PathBase phía Exam.WebApp) - dùng trùng tên sẽ bị
// giá trị mặc định (rỗng) ghi đè lại. Không có middleware dựng sẵn nào đọc header tuỳ biến này, nên set
// PathBase thủ công để mọi URL sinh ra bên trong (ReturnUrl, cookie path, <base href> ở App.razor) tự
// cộng lại "/idp". Truy cập trực tiếp (không qua proxy) không có header này nên không bị ảnh hưởng.
app.Use((context, next) =>
{
    var prefix = context.Request.Headers["X-Proxy-Base-Path"].ToString();
    if (!string.IsNullOrEmpty(prefix))
        context.Request.PathBase = prefix;

    return next();
});

app.UseRouting();
app.UseStaticFiles();
```

Lưu ý thứ tự: middleware này phải đứng trước `UseRouting()` VÀ trước `UseStaticFiles()` (để asset `_framework/...` cũng được route đúng), tức là ngay đầu pipeline, trước cả `UseSerilogRequestLogging()` cũng được (không bắt buộc nhưng để log thấy path đã cộng prefix cho dễ debug thì đặt sau logging một chút cũng không sao — không ảnh hưởng chức năng).

Middleware này **không có tác dụng gì** khi truy cập trực tiếp cổng `5001` (không có header `X-Proxy-Base-Path` → `PathBase` rỗng như cũ) — đúng ý đồ thiết kế ở mục 2.

### 4.5. `App.razor` — `<base href>` động

`src/Services/Identity/Identity.Server/Components/App.razor`, dòng 7:

```razor
<base href="/" />
```

Đổi thành:

```razor
<base href="@(HttpContext.Request.PathBase)/" />
```

Và thêm cascading parameter ở cuối file (trước `</html>`, thêm block `@code`):

```razor
@code {
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;
}
```

`HttpContext` được cascade tự động cho component gốc khi render qua `MapRazorComponents` (không cần đăng ký thêm `IHttpContextAccessor`). Khi `PathBase` rỗng (truy cập trực tiếp `:5001`), kết quả là `<base href="/" />` y hệt hiện tại — không phá vỡ đường truy cập trực tiếp.

## 5. Kiểm thử

Chạy cả 2 kịch bản (docker-compose và local `dotnet run`), với từng bước dùng **DevTools > Network**, cột "Name" phải luôn hiển thị origin `localhost:6004` xuyên suốt (trừ request `/signin-oidc` cuối cùng vốn đã đúng port đó).

1. **Login thành công**
   - Vào `http://localhost:6004` → 302 sang `/account/login` (WebApp) → 302 sang `http://localhost:6004/idp/connect/authorize?...` (không còn `:5001`).
   - Form đăng nhập hiển thị đúng CSS (nếu `<base href>` sai, trang vẫn hiện được vì CSS đang inline trong `<style>`, nhưng `_framework/blazor.web.js` sẽ 404 — mở tab Network kiểm tra request này trả 200).
   - Đăng nhập sai mật khẩu → thông báo lỗi hiển thị đúng, URL vẫn `localhost:6004/idp/Account/Login`.
   - Đăng nhập đúng → quay lại `localhost:6004/signin-oidc?code=...` → vào được trang chủ, có Role claim (kiểm tra không vỡ `OnTokenValidated`).
2. **Logout**
   - Bấm đăng xuất → 302 sang `localhost:6004/idp/connect/endsession?...` → trang "Đang đăng xuất..." hiển thị đúng → quay lại `localhost:6004/signout-callback-oidc`.
3. **Session hết hạn / deep link**
   - Mở thẳng 1 URL được bảo vệ khi chưa login → phải redirect đúng chuỗi như bước 1, không văng lỗi 404 ở `ReturnUrl`.
4. **Không phá vỡ truy cập trực tiếp**
   - `http://localhost:5001/Account/Login` (không qua proxy) vẫn tự hoạt động bình thường như trước (dùng cho `exam_api_swaggerui` client) — `<base href="/" />`, không prefix.
5. **Token vẫn hợp lệ**
   - Sau khi login qua `6004`, gọi 1 API cần auth (`exam.api`) — xác nhận 200, không phải 401 do issuer mismatch (theo phân tích mục 1, không nên xảy ra vì `IssuerUri` tĩnh, nhưng vẫn test lại cho chắc).

## 6. Rollback

Đổi `IdentityServer:PublicAuthority` (appsettings.json + docker-compose env) về lại `http://localhost:5001` là đủ để quay lại hành vi cũ ngay lập tức — các thay đổi ở YARP/Identity.Server không gây hại gì khi không được trỏ tới (route `/idp/**` chỉ tồn tại thêm, không thay route nào cũ).

## 7. Giới hạn / việc chưa làm (out of scope)

- Port `5001` của `identity.server` vẫn mở public — proxy này không "đóng" cổng đó, chỉ thêm một đường thay thế cho `Exam.WebApp`. Muốn khoá hẳn `5001` khỏi internet (chỉ giữ trong docker network nội bộ) là một thay đổi riêng, ảnh hưởng tới client `exam_api_swaggerui`, cần bàn thêm.
- `exam.api` (Swagger UI, port 5000) không nằm trong phạm vi này, vẫn redirect thẳng sang `5001` như cũ.
