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
