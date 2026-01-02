window.enterFullscreen = function () {
    const elem = document.documentElement;
    if (elem.requestFullscreen) {
        elem.requestFullscreen();
    }
};

window.exitFullscreen = function () {
    if (document.exitFullscreen) {
        document.exitFullscreen();
    }
};