import { IpcRequest } from "src/shared/IpcRequest";
import { IpcService } from "./IpcService";
import * as $ from 'jquery';
import { apiEndpointKey } from "../electron/IPC/BaseApiChannel";

const ipc = new IpcService();

$(function() {
    ipc.send<{ kernel: string }>('system-info').then(res => {
        $('#os-info').html(res.kernel);
    })
})

$('#frmLogin').on('submit', async (event) => {
    event.preventDefault();
    let req: IpcRequest = {
        params: {
            Username: $('#txtUsername').val(),
            Password: $('#txtPwd').val()
        }
    };
    const loginResponse = await ipc.sendApi("login", req);
    console.log(loginResponse)

    const bookResponse = await ipc.sendApi("book", {
        params: {
            [apiEndpointKey]: 'api/books'
        }
    });
    console.log(bookResponse)
})