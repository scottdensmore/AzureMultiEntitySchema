function FileUploadBag(fancyContainer, maxItems) {
    this.fancyContainer = fancyContainer;
    this.maxItems = maxItems;
    this.fCounter = 0;
    this.activeItems = 0;
    this.containers = [];
    // first empty element
    this.createEmptyUpload();
}
FileUploadBag.prototype.getFileCount = function() {
    return this.activeItems;
}
FileUploadBag.prototype.createEmptyUpload = function() {
    var uploadId = 'upload_' + (++this.fCounter);
    // teim
    var li = document.createElement("li");
    var item = new FileUploadSkin(li, uploadId);
    this.fancyContainer.appendChild(li);
    // save reference
    this.containers.push(item);
    item.setEmpty();
    item.onChange = Function.createDelegate(this, this.onLastItemSetted);
}
FileUploadBag.prototype.onLastItemSetted = function() {
    this.containers[this.containers.length - 1].onChange = null;
    this.activeItems++;
    if (this.fCounter >= this.maxItems) return;
    this.createEmptyUpload();
}


function FileUploadSkin(container, uploadId) {
    this.container = container;
    this.container.className = "upl";
    this.container.style.display = "none";
    
    this.fileUpload = document.createElement("input");
    this.fileUpload.id = uploadId;
    this.fileUpload.name = uploadId;
    this.fileUpload.type = "file";
    this.fileUpload.onchange = Function.createDelegate(this, this.fileUpload_onChange);
    this.container.appendChild(this.fileUpload);
    
    this.spanIconContainer = document.createElement("icon");
    this.spanIconContainer.className = "upl_iconContainer";
    this.container.appendChild(this.spanIconContainer);
    
    this.spanIcon = document.createElement("icon");
    this.spanIcon.className = "";
    this.spanIconContainer.appendChild(this.spanIcon);
    
    this.spanFilename = document.createElement("span");
    this.spanFilename.className = "upl_filename";
    this.spanFilename.appendChild(document.createTextNode(""));
    this.container.appendChild(this.spanFilename);
}
FileUploadSkin.prototype.update = function(viewData) {
    if(viewData != null) {
        this.spanIcon.className = "upl_icon_" + viewData.type;
        this.spanFilename.firstChild.nodeValue = viewData.filename;
        this.container.style.display = "block";
    } else {
        this.container.style.display = "none";
    }
}
FileUploadSkin.prototype.setEmpty = function() {
    this.spanIcon.className = "upl_icon_new";
    this.spanFilename.firstChild.nodeValue = "Add Receipt";
    this.container.style.display = "block";
}
FileUploadSkin.prototype.fileUpload_onChange = function(e) {
    var viewData = this.parseViewDataFromFilename(this.fileUpload.value);
    this.update(viewData);
    if(viewData != null && this.onChange != null) this.onChange(this.fileUpload.value);
}
FileUploadSkin.prototype.hasFile = function() {
    return this.fileUpload.value != "";
}
FileUploadSkin.prototype.parseViewDataFromFilename = function(filename) {
    filename = filename.split('\\').join('/');
    filename = filename.substring(filename.lastIndexOf('/')+1);
    var type;
    var ext = filename.substring(filename.lastIndexOf('.')+1).toLowerCase();
    switch(ext) {
        case("jpg"):
        case("jpeg"):
        case("gif"):
        case("bmp"):
        case("png"):
            type = "picture";
            break;
        default:
            type = "unknown";
            break;
    }
    return {
        filename: filename,
        type: type
    };
}

// helpers
if(Function.createDelegate == undefined) Function.createDelegate = function(a,b) { return function() { return b.apply(a,arguments) } };