import { ipcRenderer } from "electron";
import { IpcRequest, IpcResponse } from "../shared/IpcRequest";
import { IpcService } from "./IpcService";
import $ from 'jquery';
import { channels } from "../utils";

const ipcService = new IpcService();

async function login() {
    let req: IpcRequest = {
        params: {
            body: {
                Username: $('#txtUsername').val(),
                Password: $('#txtPwd').val()
            }
        }
    };
    const loginResponse = await ipcService.sendApi<IpcResponse>("login", req);
    if (loginResponse.code == 200) {
        ipcService.send(channels.menu, {
            params: {
                type: 'user'
            }
        }).then(() => {
            ipcService.sendOpenFile('../app/pages/book/book.html');
        })
    } else {
        ipcService.sendDialogError(loginResponse.data);
    }
}

$(function() {
    ipcService.send<{ kernel: string }>('system-info').then(res => {
        $('#os-info').html(res.kernel);
    })

    $('#btnExit').on('click', () => {
        ipcRenderer.send(channels.appExit)
    })

    $('#btnLogin').on('click', login)
})