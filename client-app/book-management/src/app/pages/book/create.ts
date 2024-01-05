import $ from 'jquery';
import { IpcResponse } from '../../../shared/IpcRequest';
import { apiEndpointKey, apiHost, apiMethodKey } from '../../../electron/IPC/BaseApiChannel';
import { IpcService } from '../../../app/IpcService';
import { channels, fileToBase64 } from '../../../utils';

const ipcService = new IpcService();

$(function () {
    const searchParams = new URLSearchParams(global.location.search);
    const bookId = searchParams.get('id');
    const isEdit = searchParams.has('id');
    if (isEdit) {
        ipcService.sendApi<IpcResponse>("book", {
            params: {
                [apiEndpointKey]: `api/books/${bookId}`
            }
        }).then(res => {
            if (res.code == 200) {
                $('[name="bookName"]').val(res.data.name);
                $('[name="category"]').val(res.data.category);
                $('[name="author"]').val(res.data.author);
                if (res.data.coverPicture) {
                    $('#coverImg').attr('src', `${apiHost}/api/books/download-cover?id=${res.data.id}&u=${res['extraData'].username}`);
                }
            } else {
                ipcService.sendDialogError(res.data);
            }
        });
    } else {
        $('#coverImg').hide();
    }

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
        let reqParams: any = {
            [apiEndpointKey]: 'api/books',
            [apiMethodKey]: 'fetch',
            body: body
        }
        if (isEdit) {
            reqParams['method'] = 'put';
            reqParams[apiEndpointKey] += `/${bookId}`;
        }
        const response = await ipcService.sendApi<IpcResponse>("book", {
            params: reqParams
        });
        console.log(response)
        if (response.code == 200) {
            ipcService.sendDialogInfo(`${isEdit ? 'Update' : 'Create'} successful!`).then(res => {
                if (res) { 
                    ipcService.send(channels.openFile, {
                        params: {
                            path: '../app/pages/book/book.html'
                        }
                    });
                }
            });
        } else {
            ipcService.sendDialogError(response.data);
        }
    })
})
