import $ from 'jquery';
import DataTable from 'datatables.net-bs5';
import { BookCategoryService } from '../../../app/services/book-category.service';

const categoryService = new BookCategoryService();

$(async function () {
    const response = await categoryService.listCategories();
    if (response.success) {
        let table = new DataTable('#grid', {
            data: response.data,
            order: [],
            columns: [
                { data: 'name', title: 'Name', orderable: false },
                { 
                    data: 'parentPath',
                    title: 'Parent',
                    orderable: false,
                    render: (data, type, row, meta) => {
                        if (data) {
                            const arr: string[]= data.split('.');
                            const lastId = arr[arr.length - 1];
                            return response.data.find(a => a.id == lastId)?.name ?? '-';
                        }
                        return '-';
                    },
                },
                {
                    data: 'id',
                    title: 'Action',
                    orderable: false,
                    render: (data, type, row, meta) => {
                        const editBtn = `<a href="javascript:void(0)" class="edit-book">Edit</a>`;
                        return `<div>${editBtn}</div>`;
                    },
                }
            ],
            searching: false
        })
        $('#grid tbody').on('click', '.edit-book', function () {
            let tr = $(this).closest('tr');
            let data = table.row(tr).data();
            editCategory(data.id);
        });
    } else {
        categoryService.sendDialogError(response.data)
    }

    function editCategory(id: string) {
        categoryService.sendOpenFile(categoryService.pagePaths.createCategory, null, { id });
    }

    $('#btnCreate').on('click', async () => {
        categoryService.sendOpenFile(categoryService.pagePaths.createCategory);
    })
})