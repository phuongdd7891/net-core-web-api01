
$('#frmLogin').on('submit', () => {
    var username = $('#txtUsername').val();
    var password = $('#txtPwd').val();
    window.ipcRenderer.invoke('login', username, password).then((result) => {
        console.log(result)
    });
    return false;
})

$('#btnBooks').on('click', () => {
    window.ipcRenderer.invoke('books').then((result) => {
        console.log(result)
    });
})