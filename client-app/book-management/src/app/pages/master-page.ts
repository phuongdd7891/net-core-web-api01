import $ from 'jquery';

const pageTitles = {
    './book/book.html': 'List Book',
    './book/create.html': 'Create Book'
};
const searchParams = new URLSearchParams(global.location.search);
const loadPath = searchParams.get('path');
const scriptPath = searchParams.get('script');

$.get(loadPath, (data) => {
    $('#contentWrapper').append(data);
    $(document).prop('title', pageTitles[loadPath]);
    require(scriptPath);
})
