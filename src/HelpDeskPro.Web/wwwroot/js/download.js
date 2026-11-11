// Mengunduh file yang diterima dari server Blazor (DotNetStreamReference) sebagai blob.
window.downloadFileFromStream = async function (fileName, contentType, streamRef) {
    var arrayBuffer = await streamRef.arrayBuffer();
    var blob = new Blob([arrayBuffer], { type: contentType || 'application/octet-stream' });
    var url = URL.createObjectURL(blob);
    var anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName || 'download';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
};
