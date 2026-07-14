// Port của docs/requirements/mockups/app/assets/shell.js - chỉ giữ lại phần
// hành vi cần cho app thật (toast + sidebar-toggle mobile). Phần renderShell()/
// role-switch/mock-user của bản mockup không dùng vì MainLayout.razor tự
// render topbar/sidebar dựa trên AuthorizeView + thông tin đăng nhập thật.
window.appShell = (function () {
  function toast(message, severity) {
    let el = document.getElementById("__toast");
    if (!el) {
      el = document.createElement("div");
      el.id = "__toast";
      document.body.appendChild(el);
    }
    el.className = "toast" + (severity ? " " + severity : "");
    el.textContent = message;
    // reflow để restart animation khi gọi toast liên tiếp
    void el.offsetWidth;
    el.classList.add("show");
    clearTimeout(toast._t);
    toast._t = setTimeout(() => el.classList.remove("show"), 2200);
  }

  function initSidebarToggle() {
    const toggle = document.getElementById("__sidebarToggle");
    const sidebar = document.getElementById("__sidebar");
    if (!toggle || !sidebar || toggle.dataset.wired) return;
    toggle.dataset.wired = "1";
    toggle.addEventListener("click", () => sidebar.classList.toggle("open"));
    sidebar.querySelectorAll(".nav-link").forEach(link => {
      link.addEventListener("click", () => sidebar.classList.remove("open"));
    });
  }

  // Cảnh báo rời trang khi có thay đổi chưa lưu (vd bảng Phân quyền) - trang gọi
  // setDirtyGuard(true/false) mỗi khi trạng thái dirty thay đổi.
  let dirty = false;
  window.addEventListener("beforeunload", e => {
    if (dirty) {
      e.preventDefault();
      e.returnValue = "";
    }
  });
  function setDirtyGuard(isDirty) { dirty = isDirty; }

  return { toast, initSidebarToggle, setDirtyGuard };
})();
