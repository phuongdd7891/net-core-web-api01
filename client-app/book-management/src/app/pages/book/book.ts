import { IpcRequest, IpcResponse } from "../../../shared/IpcRequest";
import { IpcService } from "../../IpcService";
import * as $ from 'jquery';
import { apiEndpointKey } from "../../../electron/IPC/BaseApiChannel";

const ipcService = new IpcService();

$(async function() {
    const bookResponse = await ipcService.sendApi<IpcResponse>("book", {
        params: {
            [apiEndpointKey]: 'api/books'
        }
    });
    if (bookResponse.code == 200) {
        $('#info').html(JSON.stringify(bookResponse))
    } else {
        ipcService.sendDialogError('',  bookResponse.data)
    }
})