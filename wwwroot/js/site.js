// Sindh Prisons PMIS - Client helpers with iframe persistence

document.addEventListener("DOMContentLoaded", function () {
  // 1. If arriving after sign out or with loggedOut parameter, purge all tokens
  const urlParams = new URLSearchParams(window.location.search);
  if (urlParams.has("loggedOut") || window.location.pathname.toLowerCase().includes("logout")) {
    try {
      sessionStorage.clear();
      localStorage.clear();
      document.cookie = "PMIS_AUTH_TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
      document.cookie = ".SindhPrisons.PMIS.Auth=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
    } catch (e) {}
  }

  // Handle signout click explicitly to purge tokens before request
  document.querySelectorAll(".sidebar-signout-btn, a[href*='Logout']").forEach(function (btn) {
    btn.addEventListener("click", function () {
      try {
        sessionStorage.clear();
        localStorage.clear();
        document.cookie = "PMIS_AUTH_TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        document.cookie = ".SindhPrisons.PMIS.Auth=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
      } catch (err) {}
    });
  });

  // 2. Password toggle on login
  const toggleBtn = document.getElementById("togglePasswordBtn");
  const passwordInput = document.getElementById("passwordInput");

  if (toggleBtn && passwordInput) {
    toggleBtn.addEventListener("click", function () {
      const isPassword = passwordInput.getAttribute("type") === "password";
      passwordInput.setAttribute("type", isPassword ? "text" : "password");
    });
  }

  // 3. Iframe token persistence for browsers blocking 3rd-party cookies
  let token = urlParams.get("t") || urlParams.get("auth");

  if (token) {
    try {
      sessionStorage.setItem("pmis_token", token);
    } catch (e) {}
  } else if (!urlParams.has("loggedOut")) {
    try {
      token = sessionStorage.getItem("pmis_token");
    } catch (e) {}
  }

  // If token is found in storage and not in URL, attach to internal navigation links and forms
  if (token && !urlParams.has("loggedOut") && !window.location.pathname.toLowerCase().includes("login")) {
    document.querySelectorAll("a[href]").forEach(function (a) {
      const href = a.getAttribute("href");
      if (href && href.startsWith("/") && !href.startsWith("//") && !href.toLowerCase().includes("logout") && !href.toLowerCase().includes("login")) {
        if (!href.includes("t=") && !href.includes("auth=")) {
          const sep = href.includes("?") ? "&" : "?";
          a.setAttribute("href", href + sep + "t=" + encodeURIComponent(token));
        }
      }
    });

    document.querySelectorAll("form").forEach(function (f) {
      const action = (f.getAttribute("action") || "").toLowerCase();
      if (!action.includes("logout") && !action.includes("login") && !f.querySelector("input[name='t']")) {
        const hidden = document.createElement("input");
        hidden.type = "hidden";
        hidden.name = "t";
        hidden.value = token;
        f.appendChild(hidden);
      }
    });
  }

  // 4. Dynamic cell loading when barrack changes in Prisoner registration
  const barrackSelect = document.getElementById("prisonerBarrackSelect");
  const cellSelect = document.getElementById("prisonerCellSelect");

  if (barrackSelect && cellSelect) {
    barrackSelect.addEventListener("change", function () {
      const barrackId = this.value;
      cellSelect.innerHTML = '<option value="">Select cell</option>';

      if (!barrackId) return;

      fetch('/Prisoners/GetCellsForBarrack?barrackId=' + encodeURIComponent(barrackId))
        .then(function (res) { return res.json(); })
        .then(function (cells) {
          cells.forEach(function (c) {
            const opt = document.createElement("option");
            opt.value = c.cellID;
            opt.textContent = "Cell " + c.cellNumber + " (Cap: " + c.capacity + ")";
            cellSelect.appendChild(opt);
          });
        })
        .catch(function (err) {
          console.error("Error loading cells:", err);
        });
    });
  }
});
