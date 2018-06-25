var code_readers = ["upc_reader", "ean_reader", "ean_8_reader"];
var radarrUrl = "";

var noPoster = "/Content/Images/poster-dark.png";

$(function () {

    if (window.localStorage) {
        radarrUrl = window.localStorage.getItem('radarrUrl');
        $('#radarrUrl').val(radarrUrl);
        $('#radarrApiKey').val(window.localStorage.getItem('radarrApiKey'));

        $('#radarrUrl').on('input', function () { window.localStorage.setItem('radarrUrl', $('#radarrUrl').val()) });
        $('#radarrApiKey').on('input', function () { window.localStorage.setItem('radarrApiKey', $('#radarrApiKey').val()) });

        $('#radarrQuality').on('input', function () {
            var option = new Option($('#radarrQuality option:selected').text(), $('#radarrQuality').val());
            window.localStorage.setItem('radarrQualityText', option.text);
            window.localStorage.setItem('radarrQualityValue', option.value);
        });

        $('#radarrFolder').on('input', function () {
            var option = new Option($('#radarrFolder option:selected').text(), $('#radarrFolder').val());
            window.localStorage.setItem('radarrFolderText', option.text);
            window.localStorage.setItem('radarrFolderValue', option.value);
        });

        var radarrQualityValue = window.localStorage.getItem('radarrQualityValue');
        var radarrQualityText = window.localStorage.getItem('radarrQualityText');

        var radarrFolderValue = window.localStorage.getItem('radarrFolderValue');
        var radarrFolderText = window.localStorage.getItem('radarrFolderText');

        if (radarrFolderValue !== null) {
            var option = new Option(radarrFolderText, radarrFolderValue);
            $('#radarrFolder').append($(option));
        }

        if (radarrQualityValue !== null) {
            var option = new Option(radarrQualityText, radarrQualityValue);
            $('#radarrQuality').append($(option));
        }
    }

    $('#radarrQualityGet').on('click', function () {
        $.ajax({
            method: "GET",
            url: radarrUrl + '/api/profile',
            data: { apiKey: $('#radarrApiKey').val() }
        }).done(function (profiles) {
            for (var i = 0; i < profiles.length; i++) {
                var exists = $("#radarrQuality option[value='" + profiles[i].id + "']").length !== 0;
                if (!exists) {
                    var option = new Option(profiles[i].name, profiles[i].id);
                    $('#radarrQuality').append($(option));
                }
            }
            var option = new Option($('#radarrQuality option:selected').text(), $('#radarrQuality').val());
            window.localStorage.setItem('radarrQualityText', option.text);
            window.localStorage.setItem('radarrQualityValue', option.value);
        });
    });

    $('#radarrFolderGet').on('click', function () {
        $.ajax({
            method: "GET",
            url: radarrUrl + '/api/rootFolder',
            data: { apiKey: $('#radarrApiKey').val() }
        }).done(function (profiles) {
            for (var i = 0; i < profiles.length; i++) {
                var exists = $("#radarrFolder option[value='" + profiles[i].id + "']").length !== 0;
                if (!exists) {
                    var option = new Option(profiles[i].path, profiles[i].id);
                    $('#radarrFolder').append($(option));
                }
            }
            var option = new Option($('#radarrFolder option:selected').text(), $('#radarrFolder').val());
            window.localStorage.setItem('radarrFolderText', option.text);
            window.localStorage.setItem('radarrFolderValue', option.value);
        });
    });

    $('#result_strip').on('click','.film', function () {
        var data = JSON.parse($(this).find('.json').text());
        var that = this;
        var folder = window.localStorage.getItem('radarrFolderText');
        var quality = window.localStorage.getItem('radarrQualityValue');
        var url = radarrUrl + '/api/movie?apikey=' + $('#radarrApiKey').val();
        $.ajax({
            method: 'POST',
            url: url,
            dataType: 'json',
            contentType: "application/json",
            data: JSON.stringify({
                title: data.title,
                qualityProfileId: quality,
                titleSlug: data.titleSlug,
                images: data.images,
                tmdbId: data.tmdbId,
                rootFolderPath: folder,
                year: data.year,
                monitored: true
            })
        }).done(function (d) {
            $(that).hide();
        });
    });

    var App = {
        init : function() {
            Quagga.init(this.state, function(err) {
                if (err) {
                    console.log(err);
                    return;
                }
                App.attachListeners();
                App.checkCapabilities();
                Quagga.start();
            });
        },
        checkCapabilities: function() {
            var track = Quagga.CameraAccess.getActiveTrack();
            var capabilities = {};
            if (typeof track.getCapabilities === 'function') {
                capabilities = track.getCapabilities();
            }
            this.applySettingsVisibility('zoom', capabilities.zoom);
            this.applySettingsVisibility('torch', capabilities.torch);
        },
        updateOptionsForMediaRange: function(node, range) {
            console.log('updateOptionsForMediaRange', node, range);
            var NUM_STEPS = 6;
            var stepSize = (range.max - range.min) / NUM_STEPS;
            var option;
            var value;
            while (node.firstChild) {
                node.removeChild(node.firstChild);
            }
            for (var i = 0; i <= NUM_STEPS; i++) {
                value = range.min + (stepSize * i);
                option = document.createElement('option');
                option.value = value;
                option.innerHTML = value;
                node.appendChild(option);
            }
        },
        applySettingsVisibility: function(setting, capability) {
            // depending on type of capability
            if (typeof capability === 'boolean') {
                var node = document.querySelector('input[name="settings_' + setting + '"]');
                if (node) {
                    node.parentNode.style.display = capability ? 'block' : 'none';
                }
                return;
            }
            if (window.MediaSettingsRange && capability instanceof window.MediaSettingsRange) {
                var node = document.querySelector('select[name="settings_' + setting + '"]');
                if (node) {
                    this.updateOptionsForMediaRange(node, capability);
                    node.parentNode.style.display = 'block';
                }
                return;
            }
        },
        initCameraSelection: function(){
            var streamLabel = Quagga.CameraAccess.getActiveStreamLabel();

            return Quagga.CameraAccess.enumerateVideoDevices()
            .then(function(devices) {
                function pruneText(text) {
                    return text.length > 30 ? text.substr(0, 30) : text;
                }
                var $deviceSelection = document.getElementById("deviceSelection");
                while ($deviceSelection.firstChild) {
                    $deviceSelection.removeChild($deviceSelection.firstChild);
                }
                devices.forEach(function(device) {
                    var $option = document.createElement("option");
                    $option.value = device.deviceId || device.id;
                    $option.appendChild(document.createTextNode(pruneText(device.label || device.deviceId || device.id)));
                    $option.selected = streamLabel === device.label;
                    $deviceSelection.appendChild($option);
                });
            });
        },
        attachListeners: function() {
            var self = this;

            self.initCameraSelection();
            $(".controls").on("click", "button.stop", function(e) {
                e.preventDefault();
                Quagga.stop();
            });

            $(".controls .reader-config-group").on("change", "input, select", function(e) {
                e.preventDefault();
                var $target = $(e.target),
                    value = $target.attr("type") === "checkbox" ? $target.prop("checked") : $target.val(),
                    name = $target.attr("name"),
                    state = self._convertNameToState(name);

                console.log("Value of "+ state + " changed to " + value);
                self.setState(state, value);
            });
        },
        _accessByPath: function(obj, path, val) {
            var parts = path.split('.'),
                depth = parts.length,
                setter = (typeof val !== "undefined") ? true : false;

            return parts.reduce(function(o, key, i) {
                if (setter && (i + 1) === depth) {
                    if (typeof o[key] === "object" && typeof val === "object") {
                        Object.assign(o[key], val);
                    } else {
                        o[key] = val;
                    }
                }
                return key in o ? o[key] : {};
            }, obj);
        },
        _convertNameToState: function(name) {
            return name.replace("_", ".").split("-").reduce(function(result, value) {
                return result + value.charAt(0).toUpperCase() + value.substring(1);
            });
        },
        detachListeners: function() {
            $(".controls").off("click", "button.stop");
            $(".controls .reader-config-group").off("change", "input, select");
        },
        applySetting: function(setting, value) {
            var track = Quagga.CameraAccess.getActiveTrack();
            if (track && typeof track.getCapabilities === 'function') {
                switch (setting) {
                case 'zoom':
                    return track.applyConstraints({advanced: [{zoom: parseFloat(value)}]});
                case 'torch':
                    return track.applyConstraints({advanced: [{torch: !!value}]});
                }
            }
        },
        setState: function(path, value) {
            var self = this;

            if (typeof self._accessByPath(self.inputMapper, path) === "function") {
                value = self._accessByPath(self.inputMapper, path)(value);
            }

            if (path.startsWith('settings.')) {
                var setting = path.substring(9);
                return self.applySetting(setting, value);
            }
            self._accessByPath(self.state, path, value);

            console.log(JSON.stringify(self.state));
            App.detachListeners();
            Quagga.stop();
            App.init();
        },
        inputMapper: {
            inputStream: {
                constraints: function(value){
                    if (/^(\d+)x(\d+)$/.test(value)) {
                        var values = value.split('x');
                        return {
                            width: {min: parseInt(values[0])},
                            height: {min: parseInt(values[1])}
                        };
                    }
                    return {
                        deviceId: value
                    };
                }
            },
            numOfWorkers: function(value) {
                return parseInt(value);
            },
            decoder: {
                readers: function(value) {
                    if (value === 'ean_extended') {
                        return [{
                            format: "ean_reader",
                            config: {
                                supplements: [
                                    'ean_5_reader', 'ean_2_reader'
                                ]
                            }
                        }];
                    }
                    return [{
                        format: value + "_reader",
                        config: {}
                    }];
                }
            }
        },
        state: {
            inputStream: {
                type : "LiveStream",
                constraints: {
                    width: 320,
                    height: 640,
                    aspectRatio: { min: 0, max: 100 },
                    facingMode: "environment" // or user
                }
            },
            locator: {
                patchSize: "medium",
                halfSample: true
            },
            numOfWorkers: 2,
            frequency: 10,
            decoder: {
                readers : code_readers
            },
            locate: true
        },
        results : []
    };

    App.init();

    Quagga.onProcessed(function(result) {
        var drawingCtx = Quagga.canvas.ctx.overlay,
            drawingCanvas = Quagga.canvas.dom.overlay;

        if (result) {
            if (result.boxes) {
                drawingCtx.clearRect(0, 0, parseInt(drawingCanvas.getAttribute("width")), parseInt(drawingCanvas.getAttribute("height")));
                result.boxes.filter(function (box) {
                    return box !== result.box;
                }).forEach(function (box) {
                    Quagga.ImageDebug.drawPath(box, {x: 0, y: 1}, drawingCtx, {color: "green", lineWidth: 2});
                });
            }

            if (result.box) {
                Quagga.ImageDebug.drawPath(result.box, {x: 0, y: 1}, drawingCtx, {color: "#00F", lineWidth: 2});
            }

            if (result.codeResult && result.codeResult.code) {
                Quagga.ImageDebug.drawPath(result.line, {x: 'x', y: 'y'}, drawingCtx, {color: 'red', lineWidth: 3});
            }
        }
    });

    Quagga.onDetected(function(result) {
        var code = result.codeResult.code;

        if (!App.results.includes(code)) {
            App.results.push(code);
            radarrUrl = $('#radarrUrl').val();
            $.ajax({
                method: "GET",
                url: "api/barcode",
                data: { barcode: code }
            }).done(function (data) {
                if (data !== undefined) {
                    var searchTerm = data.title.replace('[DVD]', '').replace('[', '(').replace(']', ')');
                    $.ajax({
                        method: "GET",
                        url: radarrUrl + '/api/movie/lookup',
                        data: { apiKey: $('#radarrApiKey').val(), term: searchTerm }
                    }).done(foundFilms);

                }
            });
        }
    });

    function foundFilms(films) {
        $('#radarrForm').hide();

        for (var i = 0; i < films.length; i++) {

            $node = $('<div class="col-6 film"><img class="img-fluid" src="" /><h4></h4><span class="json" style="display:none;"></span></div>')

            if (films[i].remotePoster == 'http://image.tmdb.org/t/p/original') {
                $node.find("img").attr("src", radarrUrl + noPoster);
            } else {
                $node.find("img").attr("src", films[i].remotePoster.replace('http:', '').replace('https:', ''));    
            }
            
            $node.find("h4").html(films[i].title);
            $node.find("span.json").html(JSON.stringify(films[i]));
            $("#result_strip").prepend($node);
            return;
        }

        
    }

});