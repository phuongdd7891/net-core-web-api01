import $ from 'jquery';
import { fileToBase64 } from '../../../utils';
import { BookService } from '../../../app/services/book.service';

const bookService = new BookService();

$(function () {
    const searchParams = new URLSearchParams(global.location.search);
    const bookId = searchParams.get('id');
    const isEdit = searchParams.has('id');
    if (isEdit) {
        bookService.getBook(bookId).then(res => {
            if (res.success) {
                $('[name="bookName"]').val(res.data.name);
                $('[name="category"]').val(res.data.category);
                $('[name="author"]').val(res.data.author);
                if (res.data.coverPicture) {
                    const imgSrc = bookService.getImageSrc(res.data.id);
                    $('#coverImg').attr('src', `${imgSrc}`);
                }
            } else {
                bookService.sendDialogError(res.data);
            }
        });
    } else {
        $('#coverImg').hide();
    }

    $('#btnCancel').on('click', () => {
        bookService.sendOpenFile(bookService.pagePaths.book)
    })

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
        const response = isEdit ? await bookService.updateBook(bookId, body) : await bookService.createBook(body);
        console.log(response)
        if (response.success) {
            bookService.sendDialogInfo(`${isEdit ? 'Update' : 'Create'} successful!`).then(res => {
                if (res == 0) { 
                    bookService.sendOpenFile(bookService.pagePaths.book);
                }
            });
        } else {
            bookService.sendDialogError(response.data);
        }
    })
})
