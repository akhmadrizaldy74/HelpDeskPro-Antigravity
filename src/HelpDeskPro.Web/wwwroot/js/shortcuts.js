window.initKeyboardShortcuts = function (dotNetHelper) {
    document.addEventListener('keydown', function (e) {
        var activeTag = document.activeElement ? document.activeElement.tagName.toLowerCase() : '';
        var isEditing = activeTag === 'input' || activeTag === 'textarea' || (document.activeElement && document.activeElement.isContentEditable);

        if (e.key === 'Escape') {
            if (isEditing) {
                document.activeElement.blur();
            }
            return;
        }

        if (isEditing) {
            return;
        }

        if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey) {
            var searchInput = document.getElementById('global-search');
            if (searchInput) {
                e.preventDefault();
                searchInput.focus();
                if (typeof searchInput.select === 'function') {
                    searchInput.select();
                }
            }
            return;
        }

        if ((e.key === 'N' || e.key === 'n') && !e.ctrlKey && !e.metaKey && !e.altKey) {
            e.preventDefault();
            if (dotNetHelper && typeof dotNetHelper.invokeMethodAsync === 'function') {
                dotNetHelper.invokeMethodAsync('OnShortcutNewTicket');
            } else {
                window.location.href = '/tickets/new';
            }
        }
    });
};

