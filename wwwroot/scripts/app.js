document.addEventListener("DOMContentLoaded", () => {
    const input = document.getElementById("html-input");
    const preview = document.getElementById("html-preview");
    const form = document.getElementById("html-form");
    if (form) {


        input.addEventListener("input", () => {
            preview.innerHTML = input.value;
        });

        form.addEventListener("submit", async (e) => {
            e.preventDefault();

            const htmlContent = input.value;
            const loader = document.getElementById("loader-overlay");

            try {
                loader.classList.remove("d-none"); // Show loader

                const response = await fetch(apiUrlwithHtml, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ html: htmlContent })
                });

                if (!response.ok) throw new Error("Failed to generate PDF");

                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);

                const a = document.createElement("a");
                a.href = url;
                a.download = "document.pdf";
                document.body.appendChild(a);
                a.click();
                a.remove();
                window.URL.revokeObjectURL(url);

            } catch (err) {
                alert("Error: " + err.message);
            } finally {
                loader.classList.add("d-none"); // Hide loader
            }
        });

    }
    // ---------- URL to PDF ----------
    const urlForm = document.getElementById("url-form");
    const urlInput = document.getElementById("url-input");
    if (urlForm) {
        

        urlForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const websiteUrl = urlInput.value.trim();
            if (!websiteUrl) {
                alert("Please enter a valid URL.");
                return;
            }

            const loader = document.getElementById("loader-overlay"); // if you have loader

            try {
                if (loader) loader.classList.remove("d-none");

                const response = await fetch(apiUrlwithUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ url: websiteUrl })
                });

                if (!response.ok) throw new Error("Failed to generate PDF");

                const blob = await response.blob();
                const pdfUrl = window.URL.createObjectURL(blob);

                const a = document.createElement("a");
                a.href = pdfUrl;
                a.download = "webpage.pdf";
                document.body.appendChild(a);
                a.click();
                a.remove();
                window.URL.revokeObjectURL(pdfUrl);

            } catch (err) {
                alert("Error: " + err.message);
            } finally {
                if (loader) loader.classList.add("d-none");
            }
        });
    }
});
