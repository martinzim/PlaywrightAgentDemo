window.demoSite = {
    toggleReleaseModal: function () {
        const modal = document.getElementById("release-modal");
        if (!modal) {
            return;
        }
        modal.classList.toggle("is-visible");
    }
};