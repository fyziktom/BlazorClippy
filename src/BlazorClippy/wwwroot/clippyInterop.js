
class BlazorClippy {
    constructor() {
    }

    load() {
        clippy.load('Clippy', agent => {
            window.clippyAgent = agent;
            agent.show();
        });
    }

    animateRandom() {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.animate();
        }
    }
    play(animation) {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.play(animation);
        }
    }
    speak(text) {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.speak(text);
        }
    }
    getAnimationsList() {
        if (window.clippyAgent != undefined) {
            return window.clippyAgent.animations();
        }
        return [];
    }
    gestureAt(x, y) {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.gestureAt(x, y);
        }
    }
    stopCurrent() {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.stopCurrent();
        }
    }
    stop() {
        if (window.clippyAgent != undefined) {
            window.clippyAgent.stop();
        }
    }
}

window.blazorClippy = new BlazorClippy()