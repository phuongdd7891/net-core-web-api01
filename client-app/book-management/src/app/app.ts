import { ipcRenderer } from "electron";
import { IpcRequest, IpcResponse } from "../shared/IpcRequest";
import { IpcService } from "./IpcService";
import $ from 'jquery';
import { channels } from "../utils";

const ipcService = new IpcService();

async function login() {
    const username = $('#txtUsername').val();
    const password = $('#txtPwd').val();
    let req: IpcRequest = {
        params: {
            body: {
                Username: username,
                Password: password
            }
        }
    };
    const loginResponse = await ipcService.sendApi<IpcResponse>("login", req);
    if (loginResponse.code == "Ok") {
        ipcService.send(channels.menu, {
            params: {
                type: 'user'
            }
        }).then(() => {
            ipcService.setUserStore({username, password, remember: $('#ckbRemember').is(':checked')});
            ipcService.sendOpenFile(ipcService.pagePaths.book);
        })
    } else {
        ipcService.sendDialogError(`${loginResponse.code}-${loginResponse.data}`);
    }
}

$(function() {
    ipcService.send<{ kernel: string }>('system-info').then(res => {
        $('#os-info').html(res.kernel);
    })
    ipcService.getUserStore().then(res => {
        if (res) {
            $('#txtUsername').val(res.username);
            $('#txtPwd').val(res.password);
            $('#ckbRemember').prop('checked', !!res.password);
        }
    });

    $('#btnExit').on('click', () => {
        ipcRenderer.send(channels.appExit)
    });

    $('#btnLogin').on('click', login);

    $('#showPwd').on('click', () => {
        const inputPwd = $('#txtPwd');
        if (inputPwd.attr('type') == 'password') {
            inputPwd.attr('type', 'text');
            $('#showPwd').children().addClass('bi-eye-slash-fill');
            $('#showPwd').children().removeClass('bi-eye-fill');
        } else {
            inputPwd.attr('type', 'password');
            $('#showPwd').children().removeClass('bi-eye-slash-fill');
            $('#showPwd').children().addClass('bi-eye-fill');
        }
    });
})