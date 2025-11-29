// Tooltip repositioning helper
document.addEventListener('DOMContentLoaded', function () {
    function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }

    function positionTooltip(wrapper) {
        var tip = wrapper.querySelector('.word-translation');
        if (!tip) return;

        // Reset inline styles to measure natural size
        tip.style.left = '';
        tip.style.top = '';
        tip.style.transform = '';

        var tipRect = tip.getBoundingClientRect();
        var wrapRect = wrapper.getBoundingClientRect();
        var margin = 18;

        // Preferred above the word
        var top = wrapRect.top - tipRect.height - margin + window.scrollY;
        var left = wrapRect.left + (wrapRect.width / 2) - (tipRect.width / 2) + window.scrollX;

        // If not enough space above, place below
        if (top < window.scrollY + 8) {
            top = wrapRect.bottom + margin + window.scrollY;
        }

        // Clamp horizontally to viewport with small padding
        var maxLeft = window.scrollX + document.documentElement.clientWidth - tipRect.width - 8;
        var minLeft = window.scrollX + 8;
        left = clamp(left, minLeft, maxLeft);

        tip.style.position = 'absolute';
        tip.style.left = left + 'px';
        tip.style.top = top + 'px';
        tip.style.transform = 'none';
        tip.style.transition = 'opacity 120ms ease, transform 120ms ease';
        tip.style.zIndex = 9999;
    }

    var wrappers = document.querySelectorAll('.word-wrapper');
    wrappers.forEach(function (w) {
        w.addEventListener('mouseenter', function () {
            var tip = w.querySelector('.word-translation');
            if (!tip) return;
            // Ensure visible then position
            tip.style.opacity = '1';
            tip.style.visibility = 'visible';
            positionTooltip(w);
        });
        w.addEventListener('mouseleave', function () {
            var tip = w.querySelector('.word-translation');
            if (!tip) return;
            tip.style.opacity = '';
            tip.style.visibility = '';
            tip.style.left = '';
            tip.style.top = '';
            tip.style.transform = '';
        });
        // Reposition on window resize/scroll
        window.addEventListener('scroll', function () { if (w.matches(':hover')) positionTooltip(w); }, { passive: true });
        window.addEventListener('resize', function () { if (w.matches(':hover')) positionTooltip(w); });
    });
});
