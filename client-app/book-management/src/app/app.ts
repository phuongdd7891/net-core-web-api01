import { ipcRenderer } from "electron";
import { IpcRequest, IpcResponse } from "../shared/IpcRequest";
import { IpcService } from "./IpcService";
import $ from 'jquery';
import { channels, storeKeys } from "../utils";

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
    if (loginResponse.code == 200) {
        ipcService.send(channels.menu, {
            params: {
                type: 'user'
            }
        }).then(() => {
            if ($('#ckbRemember').is(':checked')) {
                ipcService.setAppStore(storeKeys.userStore, {username, password});
            } else {
                ipcService.removeAppStore(storeKeys.userStore);
            }
            ipcService.sendOpenFile(ipcService.pagePaths.book);
        })
    } else {
        ipcService.sendDialogError(loginResponse.data);
    }
}

$(function() {
    ipcService.send<{ kernel: string }>('system-info').then(res => {
        $('#os-info').html(res.kernel);
    })

    ipcService.getAppStore(storeKeys.userStore).then(res => {
        if (res) {
            $('#txtUsername').val(res.username);
            $('#txtPwd').val(res.password);
            $('#ckbRemember').attr('checked', 'checked');
        }
    });

    $('#btnExit').on('click', () => {
        ipcRenderer.send(channels.appExit)
    })

    $('#btnLogin').on('click', login)
})