// NFR-002. ADR-003 chose polling over push infrastructure, so this asks the
// server for the current status and swaps in the fragment it returns.
(function () {
    const target = document.querySelector("[data-status-poll]");
    if (!target) {
        return;
    }

    const url = target.getAttribute("data-status-poll");
    const intervalMs = 10000;

    async function refresh() {
        try {
            const response = await fetch(url, { headers: { "Accept": "text/html" } });
            if (response.ok) {
                target.innerHTML = await response.text();
            }
        } catch {
            // A failed poll is not worth interrupting the page for.
        }
    }

    setInterval(refresh, intervalMs);
})();
