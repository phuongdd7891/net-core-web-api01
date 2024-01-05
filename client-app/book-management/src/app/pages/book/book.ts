import { IpcResponse } from "../../../shared/IpcRequest";
import { IpcService } from "../../IpcService";
import $ from 'jquery';
import { apiEndpointKey, apiHost } from "../../../electron/IPC/BaseApiChannel";
import DataTable from 'datatables.net-bs5';
import { channels } from "../../../utils";

const ipcService = new IpcService();

$(async function () {
    const bookResponse = await ipcService.sendApi<IpcResponse>("book", {
        params: {
            [apiEndpointKey]: 'api/books'
        }
    });
    if (bookResponse.code == 200) {
        let table = new DataTable('#grid', {
            data: bookResponse.data,
            columns: [
                { data: 'name', title: 'Name' },
                { data: 'category', title: 'Category' },
                { data: 'author', title: 'Author' },
                {
                    data: 'id',
                    title: 'Cover',
                    render: (data, type, row, meta) => {
                        return row['coverPicture'] ? `<img src="${apiHost}/api/books/download-cover?id=${data}&u=${bookResponse['extraData'].username}" style="max-width:20px;"/>` : '';
                    },
                },
                {
                    data: 'id',
                    title: 'Action',
                    orderable: false,
                    render: (data, type, row, meta) => {
                        return `<a href="javascript:void(0)" class="edit-book">Edit</a>`;
                    },
                }
            ],
            searching: false
        })
        $('#grid tbody').on('click', '.edit-book', function () {
            let tr = $(this).closest('tr');
            let data = table.row(tr).data();
            editBook(data.id);
        });
    } else {
        ipcService.sendDialogError(bookResponse.data)
    }

    function editBook(id: string) {
        ipcService.send(channels.openFile, {
            params: {
                path: '../app/pages/book/create.html',
                query: { id }
            }
        });
    }
})

$('#btnCreate').on('click', async () => {
    await ipcService.send(channels.openFile, {
        params: {
            path: '../app/pages/book/create.html'
        }
    });
})