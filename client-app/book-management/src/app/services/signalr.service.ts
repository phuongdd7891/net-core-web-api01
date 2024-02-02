import * as signalR from '@microsoft/signalr';

export class SignalRService {
    private connection: any;
    connect = function (url, token) {
        return new Promise((resolve, reject) => {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(url, {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .build();
            this.connection.start()
                .then(result => {
                    resolve(true);
                })
                .catch((err) => {
                    reject(err);
                })
        });

    };

    callMethod = function (name, data) {
        return new Promise((resolve, reject) => {
            this.connection.invoke(name, data)
                .then((result) => {
                    resolve(true);
                })
                .catch((err) => {
                    reject(err);
                })
        })

    };

    listenToMethod = function (name, callback) {
        this.connection.on(name, callback);
    };
}