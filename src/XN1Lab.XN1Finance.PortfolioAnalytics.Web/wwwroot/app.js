window.xn1financeAuthStorage = {
    getItem: function (key) {
        return window.sessionStorage.getItem(key);
    },
    setItem: function (key, value) {
        window.sessionStorage.setItem(key, value);
    },
    removeItem: function (key) {
        window.sessionStorage.removeItem(key);
    }
};

(function () {
    const bootShell = document.getElementById("xn1-boot-shell");
    const bootBar = document.getElementById("xn1-boot-bar");
    const bootPercent = document.getElementById("xn1-boot-percent");
    const bootStatus = document.getElementById("xn1-boot-status");
    const bootDetail = document.getElementById("xn1-boot-detail");

    let currentProgress = 0;
    let targetProgress = 3;
    let completed = false;

    function setText(element, value) {
        if (element) {
            element.textContent = value;
        }
    }

    function setTarget(value, status, detail) {
        targetProgress = Math.max(targetProgress, Math.min(value, 100));

        if (status) {
            setText(bootStatus, status);
        }

        if (detail) {
            setText(bootDetail, detail);
        }
    }

    function renderProgress() {
        if (currentProgress < targetProgress) {
            const distance = targetProgress - currentProgress;
            currentProgress += Math.max(.25, distance * .08);
        }

        const displayProgress = Math.min(100, Math.floor(currentProgress));

        if (bootBar) {
            bootBar.style.width = `${displayProgress}%`;
        }

        if (bootPercent) {
            bootPercent.textContent = `${displayProgress}%`;
        }

        if (!completed) {
            window.requestAnimationFrame(renderProgress);
        }
    }

    function scheduleSmoothMilestones() {
        const milestones = [
            [180, 8, "Preparing workspace", "Starting finance application"],
            [700, 18, "Checking resources", "Reading application manifest"],
            [1600, 34, "Loading framework", "Loading runtime files"],
            [2800, 52, "Loading modules", "Preparing finance workspace"],
            [4200, 72, "Starting application", "Initializing account session"],
            [5600, 88, "Starting application", "Almost ready"]
        ];

        milestones.forEach(function (milestone) {
            window.setTimeout(function () {
                if (!completed) {
                    setTarget(milestone[1], milestone[2], milestone[3]);
                }
            }, milestone[0]);
        });
    }

    async function startBlazor() {
        renderProgress();
        scheduleSmoothMilestones();

        try {
            setTarget(6, "Preparing workspace", "Reading application manifest");
            await Blazor.start();
            setTarget(100, "Ready", "Opening workspace");
            currentProgress = 100;
            completed = true;
            renderProgress();
        } catch (error) {
            completed = true;
            if (bootShell) {
                bootShell.classList.add("is-error");
            }

            setText(bootStatus, "Could not start XN1Finance Portfolio Analytics");
            setText(bootDetail, "Refresh the page and try again.");
            console.error(error);
        }
    }

    if (window.Blazor) {
        startBlazor();
    } else {
        window.addEventListener("DOMContentLoaded", startBlazor, { once: true });
    }
})();

