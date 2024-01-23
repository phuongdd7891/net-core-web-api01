import $ from 'jquery';
import { BookCategoryService } from '../../../app/services/book-category.service';
import { formatCategoryPath } from '../../../utils';

const categoryService = new BookCategoryService();

$(async function () {
    const searchParams = new URLSearchParams(global.location.search);
    const catId = searchParams.get('id');
    let editCat: any = null;
    if (catId) {
        const catRes = await categoryService.getCategory(catId);
        if (catRes.success) {
            editCat = catRes.data;
            $('#txtName').val(editCat.name);
        } else {
            categoryService.sendDialogError(catRes.data);
        }
    }
    categoryService.listCategories().then(res => {
        if (res.success) {
            if (res.data.length > 0) {
                res.data.forEach(c => {
                    $('#ddlParent').append(`<option value='${c.id}'>${formatCategoryPath(c.parentPath)}${c.name}</option>`);
                });
                if (editCat?.parentPath) {
                    const parents = editCat.parentPath.split('.');
                    $('#ddlParent').val(parents[parents.length - 1]);
                }
            }
        } else {
            categoryService.sendDialogError(res.data);
        }
    })

    $('#btnCancel').on('click', () => {
        categoryService.sendOpenFile(categoryService.pagePaths.category)
    })

    $('#frmCreate').on('submit', async (event) => {
        event.preventDefault();
        const data = new URLSearchParams($('#frmCreate').serialize());
        let body: any = {
            Name: data.get('categoryName'),
            Parent: data.get('categoryParent'),
        }
        if (catId) {
            body['Id'] = catId;
        }
        const response = await categoryService.createCategory(body);
        if (response.success) {
            categoryService.sendDialogInfo(`${catId ? 'Update' : 'Create'} successful!`).then(res => {
                if (res == 0) { 
                    categoryService.sendOpenFile(categoryService.pagePaths.category);
                }
            });
        } else {
            categoryService.sendDialogError(response.data);
        }
    })
})
