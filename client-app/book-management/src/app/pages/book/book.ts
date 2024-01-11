import $ from 'jquery';
import DataTable from 'datatables.net-bs5';
import { BookService } from "../../../app/services/book.service";
import moment from 'moment';

const bookService = new BookService();

$(async function () {
    const bookResponse = await bookService.listBooks();
    if (bookResponse.success) {
        let table = new DataTable('#grid', {
            data: bookResponse.data.map((a, idx) => ({
                ...a,
                order: idx + 1
            })),
            columns: [
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
                },
                {
                    data: 'id',
                    title: 'Action',
                    orderable: false,
                    render: (data, type, row, meta) => {
                        const editBtn = `<a href="javascript:void(0)" class="edit-book">Edit</a>`;
                        const deleteBtn = `<a href="javascript:void(0)" class="delete-book">Delete</a>`;
                        return `<div>${editBtn} | ${deleteBtn}</div>`;
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
    } else {
        bookService.sendDialogError(bookResponse.data)
    }

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

    $('#btnCreate').on('click', async () => {
        bookService.sendOpenFile(bookService.pagePaths.createBook);
    })
})