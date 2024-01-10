import $ from 'jquery';

const pageTitles = {
    'book.html': 'List Book',
    'create.html': 'Create Book'
};
const searchParams = new URLSearchParams(global.location.search);
const loadPath = searchParams.get('path');
const scriptPath = searchParams.get('script');

$.get(loadPath, (data) => {
    $('#contentWrapper').append(data);
    const pathName = loadPath.substring(loadPath.lastIndexOf('/') + 1);
    $(document).prop('title', pageTitles[pathName]);
    require(scriptPath);
})
