window.urAnimations = {
    animateAlongPath: function (selector, waypoints, durationMs, fadeOut) {
        const el = document.querySelector(selector);
        if (!el || waypoints.length < 2) return;

        const last = waypoints.length - 1;
        const keyframes = waypoints.map((wp, i) => {
            const kf = { transform: `translate(${wp.x}px, ${wp.y}px)` };
            if (fadeOut) {
                kf.opacity = 1 - (i / last);
            }
            return kf;
        });

        el.animate(keyframes, {
            duration: durationMs,
            easing: "linear",
            fill: fadeOut ? "forwards" : "none"
        });
    }
};
