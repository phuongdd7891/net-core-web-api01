import { IpcRequest, IpcResponse } from "../shared/IpcRequest";
import { IpcService } from "./IpcService";
import * as $ from 'jquery';

const ipcService = new IpcService();

$(function() {
    ipcService.send<{ kernel: string }>('system-info').then(res => {
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
    const loginResponse = await ipcService.sendApi<IpcResponse>("login", req);
    if (loginResponse.Code == 200) {
        ipcService.send('menu', {
            params: {
                type: 'user'
            }
        }).then(async () => {
            await ipcService.send('wd', {
                params: {
                    path: '../app/pages/book/book.html'
                }
            });
        })
    } else {
        ipcService.sendDialogError('', loginResponse.Data);
    }
})