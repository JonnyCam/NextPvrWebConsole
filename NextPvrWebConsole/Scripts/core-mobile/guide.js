﻿/// <reference path="../core/jquery-1.8.2.js" />
/// <reference path="../functions.js" />
/// <reference path="../apihelper.js" />
/// <reference path="../core/knockout-2.1.0.debug.js" />
/// <reference path="../core/modernizr-2.6.2.js" />

var currentDay;
var currentChannelGroupIndex = 0;
var initialLoadDone = false;
var channelGroups = [{ Name: 'All Channels', Id: ''}];

$(function () {

    $('#guidePrevDay').live('click', function () {
        if (!currentDay)
            return;
        var date = new Date();
        date.setHours(0, 0, 0, 0);
        if (currentDay <= date)
            return;
        date.setDate(currentDay.getDate() - 1);
        setEpgDate(date);
    });

    $('#guideNextDay').live('click', function () {
        if (!currentDay)
            return;
        var date = new Date();
        date.setHours(0, 0, 0, 0);
        date.setDate(currentDay.getDate() + 1);
        setEpgDate(date);
    });

    $('#channelGroupNext').live('click', function () {
        currentChannelGroupIndex += 1;
        if (currentChannelGroupIndex >= channelGroups.length)
            currentChannelGroupIndex = 0;
        refreshEpgData();
    });
    $('#channelGroupPrev').live('click', function () {
        currentChannelGroupIndex -= 1;
        if (currentChannelGroupIndex < 0)
            currentChannelGroupIndex = channelGroups.length - 1;
        refreshEpgData();
    });

    $('#pageGuide').live('pageshow', function (event) {
        console.log('showing guide page');
        if (!initialLoadDone) {
            $.mobile.showPageLoadingMsg();
            initialLoadEpgPage();
        }
    });
});

function initialLoadEpgPage() {
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    refreshChannelGroups(function () { setEpgDate(today); });    
    initialLoadDone = true;
}

function setEpgDate(newDate) {
    currentDay = newDate
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    var tomorrow = new Date();
    tomorrow.setHours(0, 0, 0, 0);
    tomorrow.setDate(today.getDate() + 1);
    var twodaystime = new Date();
    twodaystime.setHours(0, 0, 0, 0);
    twodaystime.setDate(today.getDate() + 2);

    var text = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"][currentDay.getDate()] + ", " +
               ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"][currentDay.getMonth()] + " " + currentDay.getDate();

    if (currentDay >= today && currentDay < tomorrow)
        text = 'Today';
    else if (currentDay >= tomorrow && currentDay < twodaystime)
        text = 'Tomorrow';
    $('#epgDate').html(text);
    refreshEpgData();
}

function refreshChannelGroups(callback) {
    api.getJSON('channelgroups', null, function (data) {
        channelGroups = [{ Name: 'All Channels', Id: ''}];
        console.log(data);
        for (var i = 0; i < data.length; i++)
            channelGroups.push(data[i]);
        if (callback)
            callback();
    });
}

function refreshEpgData() {
    console.log('refreshEpgData()');

    $.mobile.showPageLoadingMsg();

    console.log(channelGroups);
    console.log('currentChannelGroupIndex: ' + currentChannelGroupIndex);
    $('#channelGroupName').text(channelGroups[currentChannelGroupIndex].Name);

    var id = channelGroups[currentChannelGroupIndex].Id;
    if(id == null)
        id = channelGroups[currentChannelGroupIndex].Name;

    var epg = $('#epg');
    loadEpgData(currentDay, id, function (channels, totalMinutes) {
        epg.get(0).innerHTML = channels;

        epg.niceScroll();


        var timeline = $('#timeline');
        var channelicons = $('#channelicons');
        var scroller = epg.getNiceScroll()[0];
        scroller.scrollstart(function (info) {
            channelicons.css('left', (info.end.x) + 'px');
            timeline.css('top', (info.end.y) + 'px');
        });
        scroller.scrollend(function (info) {
            channelicons.css('left', (info.current.x) + 'px');
            timeline.css('top', (info.current.y) + 'px');
        });
        scroller.scrollcancel(function (info) {
            channelicons.css('left', (info.current.x) + 'px');
            timeline.css('top', (info.current.y) + 'px');
        });
        scroller.scroll(function (info) {
            console.log('scroll');
            console.log(info);
            if(info.end.x > info.current.x)
                channelicons.css('left', (info.current.x) + 'px');
            else
                channelicons.css('left', (info.end.x) + 'px');
            if(info.end.y > info.current.y)
                timeline.css('top', (info.current.y) + 'px');
            else
                timeline.css('top', (info.end.y) + 'px');
        });

        //$('#epg').html(channels);

        //$('#programList span').bind('taphold', function () {
        //    console.log('taphold!');
        //});
        $.mobile.hidePageLoadingMsg();
    });
}

function loadEpgData(startUtc, groupName, callback) {
    console.log('loading epg data: ');

    $.get('/guide/epg?date=' + $.format.date(startUtc, 'yyyy-MM-dd') + '&group=' + encodeURIComponent(groupName) + '&rand=' + Math.random(), null, function (data, textStatus, jqXHR) {
        if (callback)
            callback(data, 24 * 60);
    });
}