import $ from 'jquery';
import * as bootstrap from 'bootstrap';
import { channels } from '../../utils';
import { IpcService } from '../IpcService';
import moment from 'moment';

const pageTitles = {
    './book/book.html': 'List Book',
    './book/create.html': 'Create Book'
};
const searchParams = new URLSearchParams(global.location.search);
const loadPath = searchParams.get('path');
const scriptPath = searchParams.get('script');
const ipcService = new IpcService();

$.get(loadPath, (data) => {
    $('#contentWrapper').append(data);
    $(document).prop('title', pageTitles[loadPath]);
    require(scriptPath);
})

$(async function () {
    var userNotifs = await ipcService.getUserNotifications();
    var popoverEl = document.querySelector('[data-bs-toggle="popover"]');
    var content = '';
    userNotifs?.forEach(a => {
        content += `<p class="noti-item m-0">${a.message}<br/><i class="time">@${moment(a.time).format('YYYY-MM-DD HH:mm')}</i></p>`;
    });
    var popover = new bootstrap.Popover(popoverEl, {
        container: 'body',
        placement: 'bottom',
        html: true,
        content: content || '<span>No message</span>'
    });
    $('#countNotif').text(userNotifs?.length ?? 0);
    ipcService.getRenderer().on(channels.notify, (_, message) => {
        if (message) {
            const popover = bootstrap.Popover.getInstance(popoverEl);
            if (userNotifs?.length == 0) {
                popover._config.content = '';
            }
            popover._config.content += `<p class="noti-item m-0">${message}<br/><i class="time">@${moment().format('YYYY-MM-DD HH:mm')}</i></p>`;
            $('#popNotifDot').removeClass('invisible');
            ipcService.setUserNotifications(message);
            $('#countNotif').text(userNotifs?.length + 1);
        }
    });
    popoverEl.addEventListener("shown.bs.popover", function () {
        $('#popNotifDot').addClass('invisible');
    });
})