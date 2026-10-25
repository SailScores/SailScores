// Live preview on the results view create and edit pages. While the preview panel is open,
// the form (with its unsaved settings) is posted to the Preview action whenever a setting
// changes, and the returned results table replaces the panel's content.
// Markup comes from Views/SeriesResultsTemplate/_PreviewPanel.

function initResultsViewPreview(
    panel: HTMLElement,
    form: HTMLFormElement,
    content: HTMLElement,
    url: string): void {
    let isOpen = false;
    let timer: number | undefined;
    let requestNumber = 0;

    async function refresh(): Promise<void> {
        const thisRequest = ++requestNumber;
        try {
            const response = await fetch(url, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) {
                throw new Error(response.statusText);
            }
            const html = await response.text();
            // A later change may have been answered first.
            if (thisRequest === requestNumber) {
                content.innerHTML = html;
            }
        } catch {
            if (thisRequest === requestNumber) {
                content.innerHTML = '<p class="text-danger mb-0">The preview could not be loaded.</p>';
            }
        }
    }

    function scheduleRefresh(): void {
        if (!isOpen) {
            return;
        }
        window.clearTimeout(timer);
        timer = window.setTimeout(refresh, 300);
    }

    // Room below the form so the panel doesn't cover the last settings and the buttons.
    function setPagePadding(): void {
        document.body.style.paddingBottom = isOpen ? `${panel.offsetHeight}px` : "";
    }

    panel.addEventListener("shown.bs.offcanvas", () => {
        isOpen = true;
        setPagePadding();
        refresh();
    });
    panel.addEventListener("hidden.bs.offcanvas", () => {
        isOpen = false;
        setPagePadding();
    });
    window.addEventListener("resize", setPagePadding);

    // Links that reload the page (reset to defaults) keep the panel open on the same series.
    document.querySelectorAll<HTMLAnchorElement>("a[data-keeps-preview]").forEach(link => {
        link.addEventListener("click", () => {
            if (!isOpen) {
                return;
            }
            const target = new URL(link.href, window.location.href);
            target.searchParams.set("preview", "true");
            const series = form.querySelector<HTMLSelectElement>("#previewSeriesId");
            if (series?.value) {
                target.searchParams.set("previewSeriesId", series.value);
            }
            link.href = target.toString();
        });
    });

    const params = new URLSearchParams(window.location.search);
    if (params.get("preview") === "true") {
        const series = form.querySelector<HTMLSelectElement>("#previewSeriesId");
        const seriesId = params.get("previewSeriesId");
        if (series && seriesId && Array.from(series.options).some(o => o.value === seriesId)) {
            series.value = seriesId;
        }
        document.querySelector<HTMLElement>('[data-bs-target="#resultsViewPreview"]')?.click();
    }

    form.addEventListener("change", scheduleRefresh);
    form.addEventListener("input", event => {
        // Text boxes (headers, order) as they're typed; selects and checkboxes use change.
        if (event.target instanceof HTMLInputElement && event.target.type !== "checkbox") {
            scheduleRefresh();
        }
    });
}

(function () {
    const panel = document.getElementById("resultsViewPreview");
    const form = panel?.closest("form");
    const content = panel?.querySelector<HTMLElement>("[data-preview-content]");
    const url = panel?.dataset.previewUrl;
    if (panel && form && content && url) {
        initResultsViewPreview(panel, form, content, url);
    }
})();
