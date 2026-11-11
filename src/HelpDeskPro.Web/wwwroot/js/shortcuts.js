window.initKeyboardShortcuts = function (dotNetHelper) {
    // Simpan referensi terbaru; listener hanya dipasang sekali agar tidak ganda.
    window.__hdpShortcutHelper = dotNetHelper;
    if (window.__hdpShortcutsBound) {
        return;
    }
    window.__hdpShortcutsBound = true;

    document.addEventListener('keydown', function (e) {
        var dotNetHelper = window.__hdpShortcutHelper;
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
                dotNetHelper.invokeMethodAsync('OnShortcutNewTicket').catch(function () { window.location.href = '/tickets/new'; });
            } else {
                window.location.href = '/tickets/new';
            }
        }
    });
};

