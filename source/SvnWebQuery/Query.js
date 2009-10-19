function toggleRevisionRange() {
    var revision = document.getElementById('_revision');
    var optGroup = document.getElementById('_revisionOptions');
    if (revision.style.display == "none") {
        revision.style.display = "";
        optGroup.style.display = "none";
        revision.value = "";
    }
    else {
        revision.style.display = "none";
        revision.value = "$hidden$";
        optGroup.style.display = "";
    }
}              
