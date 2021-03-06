﻿/// <reference path="../functions.js" />
/// <reference path="../apihelper.js" />
/// <reference path="../core/jquery-1.8.2.js" />
/// <reference path="../core/jquery-ui-1.9.0.js" />
/// <reference path="../core/knockout-2.2.0.js" />
/// <reference path="../api-wrappers/recordings.js" />

$(function () {
    function UpcomingRecordingsViewModel() {
        // Data
        var self = this;

        self.upcomingRecordings = ko.observableArray([]);

        self.stop = function (listing) {
            alert('No implemented yet.');
        };

        var refreshUpcomingRecordings = function () {
            api.getJSON("recordings/getupcoming", null, function (allData) {
                var mapped = $.map(allData, function (item) { return new Recording(item) });
                self.upcomingRecordings(mapped);
            });
        };
        refreshUpcomingRecordings();
    }

    if ($('#upcoming-recordings').length > 0)
        ko.applyBindings(new UpcomingRecordingsViewModel(), $('#upcoming-recordings').get(0));
});
