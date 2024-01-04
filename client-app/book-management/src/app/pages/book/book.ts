import { IpcResponse } from "../../../shared/IpcRequest";
import { IpcService } from "../../IpcService";
import $ from 'jquery';
import { apiEndpointKey } from "../../../electron/IPC/BaseApiChannel";
import DataTable from 'datatables.net-bs5';
import { channels } from "../../../utils";

const ipcService = new IpcService();

$(async function() {
    const bookResponse = await ipcService.sendApi<IpcResponse>("book", {
        params: {
            [apiEndpointKey]: 'api/books'
        }
    });
    if (bookResponse.code == 200) {
        let table = new DataTable('#grid', {
            data: bookResponse.data,
            columns: [
                { data: 'bookName', title: 'Name' },
                { data: 'category', title: 'Category' },
                { data: 'author', title: 'Author' }
            ],
            searching: false
        })
    } else {
        ipcService.sendDialogError(bookResponse.data)
    }
})

$('#btnCreate').on('click',async () => {
    await ipcService.send(channels.openFile, {
        params: {
            path: '../app/pages/book/create.html'
        }
    });
})