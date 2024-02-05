import $ from 'jquery';
import * as bootstrap from 'bootstrap';
import { channels, dateTimeFormat } from '../../utils';
import { IpcService } from '../IpcService';
import moment, { Moment } from 'moment';

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
    var emptyMsg = '<span>No message</span>';
    var popoverEl = document.querySelector('[data-bs-toggle="popover"]');
    var popover = new bootstrap.Popover(popoverEl, {
        container: 'body',
        placement: 'bottom',
        html: true,
        content: emptyMsg
    });
    var notiCount = await loadMessages();
    $('#countNotif').text(notiCount);
    ipcService.getRenderer().on(channels.notify, (_, message) => {
        if (message) {
            if (notiCount == 0) {
                popover._config.content = '';
            }
            let element = createMsgElement(message, moment(), notiCount);
            popover._config.content += element;
            $('#popNotifDot').removeClass('invisible');
            ipcService.setUserNotifications(message);
            notiCount += 1;
            $('#countNotif').text(notiCount);
            if (popover._isShown()) {
                popover.show();
            }
        }
    });
    popoverEl.addEventListener("shown.bs.popover", function () {
        $('#popNotifDot').addClass('invisible');
        $('.remove-msg').on('click', async function () {
            var index = Number($(this).attr('id'));
            ipcService.removeUserNotifications(index);
            await loadMessages();
            notiCount -= 1;
            $('#countNotif').text(notiCount);
            $(this).parent().remove();
            if (notiCount == 0) {
                popover._config.content = emptyMsg;
                popover.show();
            }
        });
    });

    async function loadMessages() {
        const data = await ipcService.getUserNotifications();
        if (data?.length > 0) {
            let content = '';
            data.forEach((a, i) => {
                content += createMsgElement(a.message, moment(a.time), i);
            });
            popover._config.content = content;
        } else {
            popover._config.content = emptyMsg;
        }
        return data?.length ?? 0;
    }

    function createMsgElement(message: string, time: Moment, index: number) {
        let element = `<div class="noti-item d-flex flex-wrap">`;
        element += `<div class="fw-bold">${message}</div>`;
        element += `<a class="cursor-pointer remove-msg text-secondary ms-auto" id="${index}"><i class="bi bi-x-circle-fill"></i></a> `;
        element += `<div class="w-100"><i class="time">@${time.format(dateTimeFormat.YYYYMMDD_HHmm)}</i></div>`;
        element += '</div>';
        return element;
    }
})