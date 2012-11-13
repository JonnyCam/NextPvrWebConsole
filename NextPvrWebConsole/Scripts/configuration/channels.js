﻿/// <reference path="core/jquery-1.8.2.js" />
/// <reference path="core/jquery-ui-1.9.0.js" />
/// <reference path="../core/knockout-2.2.0.js" />
/// <reference path="../functions.js" />
/// <reference path="../apihelper.js" />

$(function () {
    function ChannelsViewModel() {
        var self = this;

        self.channels = ko.observableArray(channels);

        self.remove = function (item) {
            self.channels.remove(item);
        };

        self.importMissing = function () {
            api.getJSON('Channels/GetMissingChannels', null, function (results) {
                $.each(results, function (i, ele) {
                    if($('#channel-' + ele.Oid).length == 0)
                        self.channels.push(new Channel(ele));
                });
                $('#configuration-tab-channels .enabled > input[type=checkbox]').iButton();
            });
        };

        self.save = function () {
            var channels = new Array();
            $.each(self.channels(), function (i, ele) {
                ele.enabled($('#channel-' + ele.oid() + ' .enabled :checked').length > 0);
                channels.push(ele.toApiObject());
            });
            ajax.postJSON('Configuration/UpdateChannels',
                {
                    Channels: channels
                }
            );
        };
    }
    ko.applyBindings(new ChannelsViewModel(), $('#configuration-tab-channels').get(0));
    $('#configuration-tab-channels input[type=checkbox]').iButton();

    $('#channels-table').on('keypress', 'input[type=number]', function (e) {
        if (e.which == 13) {
            var nextTr = $(this).closest('tr').next();
            nextTr.find('input[type=number]').focus();
        }
    });
});