
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
    downloadText(data, filename) {
        var text = data;
        //text = text.replace(/\n/g, "\r\n"); // To retain the Line breaks.
        var blob = new Blob([text], { type: "text/plain" });
        var anchor = document.createElement("a");
        anchor.download = filename;
        anchor.href = window.URL.createObjectURL(blob);
        anchor.target = "_blank";
        anchor.style.display = "none"; // just to be safe!
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
    }
    copyToClipboard(text) {
        navigator.clipboard.writeText(text);
    }
    MermaidInitialize() {
        mermaid.initialize({
            startOnLoad: true,
            securityLevel: "loose",
            // Other options.
        });
        var mindmap = window["mermaid-mindmap"];
        mermaid.registerExternalDiagrams([mindmap]);
    }
    MermaidRender() {
        mermaid.init();
    }
}

window.blazorClippy = new BlazorClippy()