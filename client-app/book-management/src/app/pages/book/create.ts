import $ from 'jquery';
import { IpcResponse } from '../../../shared/IpcRequest';
import { apiEndpointKey, apiMethodKey } from '../../../electron/IPC/BaseApiChannel';
import { IpcService } from '../../../app/IpcService';
import { fileToBase64 } from '../../../utils';

const ipcService = new IpcService();

$(function () {
    $('#frmCreate').on('submit', async (event) => {
        event.preventDefault();
        const data = new URLSearchParams($('#frmCreate').serialize());
        let body: any = {
            'Data.BookName': data.get('bookName'),
            'Data.Author': data.get('author'),
            'Data.Category':  data.get('category')
        }
        const fileUpload = ($('#fileCover')[0] as HTMLInputElement).files?.[0];
        if (fileUpload) {
            const file = await fileToBase64(fileUpload);
            body['FileData'] = file;
        }
        const response = await ipcService.sendApi<IpcResponse>("book", {
            params: {
                [apiEndpointKey]: 'api/books',
                [apiMethodKey]: 'fetch',
                body: body
            }
        });
        console.log(response)
        if (response.code == 200) {
            ipcService.sendDialogInfo('Create successful!')
        } else {
            ipcService.sendDialogError(response.data);
        }
    })
})
