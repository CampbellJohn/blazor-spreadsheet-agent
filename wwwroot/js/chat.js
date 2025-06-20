// Chat functionality for the Blazor chat interface
window.chat = {
    // Scroll the chat container to the bottom
    scrollToBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
            // Smooth scroll
            element.scrollTo({
                top: element.scrollHeight,
                behavior: 'smooth'
            });
        }
    },

    // Focus the chat input
    focusInput: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
        }
    }
};
