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
    $('#info').html(JSON.stringify(bookResponse))
})