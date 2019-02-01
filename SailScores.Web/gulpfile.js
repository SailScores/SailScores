/// <binding BeforeBuild='scripts' AfterBuild='typescript' Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify"),
    merge = require('merge-stream'),
    sass = require("gulp-sass");

var paths = {
    webroot: "./wwwroot/"
};

paths.js = paths.webroot + "js/**/*.js";
paths.minJs = paths.webroot + "js/**/*.min.js";
paths.css = paths.webroot + "css/**/*.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatJsDest = paths.webroot + "js/site.min.js";
paths.concatCssDest = paths.webroot + "css/site.min.css";
paths.concatTsDest = paths.webroot + "scripts/**.*";
paths.scripts = ['scripts/**/*.js', 'scripts/**/*.ts', 'scripts/**/*.map'];

gulp.task("clean:js", function (cb) {
    rimraf(paths.concatJsDest, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.concatCssDest, cb);
});

gulp.task("clean:ts", function (cb) {
    rimraf(paths.concatTsDest, cb);
});

gulp.task("clean", gulp.parallel("clean:js", "clean:css", "clean:ts"));

gulp.task("min:js", function () {
    return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
        .pipe(concat(paths.concatJsDest))
        .pipe(uglify())
        .pipe(gulp.dest("."));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min", gulp.parallel("min:js", "min:css"));


// Dependency Dirs
var deps = {
    "jquery": {
        "dist/*": ""
    },
    "popper.js": {
        "dist/**/*": ""
    },
    "bootstrap": {
        "dist/**/*": ""
    },
    // ...

};

gulp.task("scripts", function () {

    var streams = [];

    for (var prop in deps) {
        console.log("Prepping Scripts for: " + prop);
        for (var itemProp in deps[prop]) {
            streams.push(gulp.src("node_modules/" + prop + "/" + itemProp)
                .pipe(gulp.dest("wwwroot/vendor/" + prop + "/" + deps[prop][itemProp])));
        }
    }

    return merge(streams);

});

gulp.task('typescript', function () {
    return gulp.src(paths.scripts).pipe(gulp.dest('wwwroot/scripts'));
});

gulp.task("sass", function () {
    return gulp.src('custom.scss')
        .pipe(sass())
        .pipe(gulp.dest('wwwroot/css'));
});