CKEDITOR.plugins.add('base64image', {
    requires: 'filetools',
    init: function (editor) {
        CKEDITOR.tools.extend(CKEDITOR.fileTools, {
            createBase64File: function (file) {
                return new CKEDITOR.fileTools.File(file, {
                    convertToBase64: function () {
                        var reader = new FileReader();
                        reader.onload = function () {
                            file.base64 = reader.result.split(',')[1];
                            editor.fire('fileUploadResponse', {
                                file: file,
                                data: {
                                    fileName: file.name,
                                    uploaded: 1,
                                    url: 'data:' + file.type + ';base64,' + file.base64
                                }
                            });
                        };
                        reader.readAsDataURL(file);
                    }
                });
            }
        });

        editor.on('fileUploadRequest', function (evt) {
            var fileLoader = evt.data.fileLoader,
                file = fileLoader.file;
            if (file) {
                CKEDITOR.fileTools.createBase64File(file).convertToBase64();
                evt.stop();
            }
        });
    }
});
