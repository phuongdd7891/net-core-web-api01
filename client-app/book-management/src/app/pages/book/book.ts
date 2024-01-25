import $ from 'jquery';
import DataTable from 'datatables.net-bs5';
import { BookService } from "../../../app/services/book.service";
import moment from 'moment';
import { apiHost } from '../../../electron/IPC/BaseApiChannel';

const bookService = new BookService();

$(async function () {
    let table = new DataTable('#grid', {
        serverSide: true,
        paging: true,
        pageLength: 15,
        ajax: function (data: any, callback, settings) {
            $.ajax({
                url: `${apiHost}/api/books`,
                data: {
                    skip: data.start,
                    limit: data.length
                },
                success: (res) => {
                    callback({
                        data: res.data.list.map((a, idx) => ({
                            ...a,
                            order: idx + 1 + data.start
                        })),
                        recordsTotal: res.data.total,
                        recordsFiltered: res.data.total
                    });
                },
                error: (err) => {
                    bookService.sendDialogError(err.responseJSON?.data);
                },
                beforeSend: () => {
                    bookService.showLoader(true);
                },
                complete: () => {
                    bookService.showLoader(false);
                }
            });
        },
        order: [],
        columns: [
            {
                data: 'id',
                title: '',
                orderable: false,
                render: (data, type, row, meta) => {
                    const editBtn = `<a href="javascript:void(0)" class="edit-book p-1"><i class="bi bi-pencil"></i></a>`;
                    const deleteBtn = `<a href="javascript:void(0)" class="delete-book p-1"><i class="bi bi-trash"></i></a>`;
                    const copyBtn = `<a href="javascript:void(0)" class="copy-book p-1"><i class="bi bi-copy"></i></a>`;
                    return `<div class="d-flex">${editBtn} | ${deleteBtn} | ${copyBtn}</div>`;
                },
            },
            { data: 'order', title: 'No.' },
            { data: 'name', title: 'Name' },
            { data: 'category', title: 'Category' },
            { data: 'author', title: 'Author' },
            {
                data: 'createdDate',
                title: 'Create Time',
                render: function (data, type, row, meta) {
                    return data ? moment.utc(data).local().format('YYYY-MM-DD HH:mm') : '';
                }
            },
            {
                data: 'id',
                title: 'Cover',
                render: (data, type, row, meta) => {
                    const imgSrc = bookService.getImageSrc(data);
                    return row['coverPicture'] ? `<img src="${imgSrc}" style="max-width:20px;"/>` : '';
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
    $('#grid tbody').on('click', '.delete-book', function () {
        let tr = $(this).closest('tr');
        let data = table.row(tr).data();
        deleteBook(data.id, data.name);
    });
    $('#grid tbody').on('click', '.copy-book', function () {
        let tr = $(this).closest('tr');
        let data = table.row(tr).data();
        copyBook(data.id);
    });

    function editBook(id: string) {
        bookService.sendOpenFile(bookService.pagePaths.createBook, null, { id });
    }

    function deleteBook(id: string, name: string) {
        bookService.sendDialogInfo(`Are you sure to delete "${name}"?`, '', 'question', ['Yes', 'No']).then(async (res) => {
            if (res == 0) {
                const deleteResponse = await bookService.deleteBook(id);
                if (deleteResponse.success) {
                    global.location.reload();
                } else {
                    bookService.sendDialogError(deleteResponse.data)
                }
            }
        })
    }

    function copyBook(id: string) {
        bookService.sendDialogInfo(`Are you sure to copy more 10 books?`, '', 'question', ['Yes', 'No']).then(async (res) => {
            if (res == 0) {
                const res = await bookService.copyBook(id, 10);
                if (res.success) {
                    global.location.reload();
                } else {
                    bookService.sendDialogError(res.data)
                }
            }
        });
    }

    $('#btnCreate').on('click', async () => {
        bookService.sendOpenFile(bookService.pagePaths.createBook);
    })

    $('#btnReload').on('click', async () => {
        table.ajax.reload();
    })

})