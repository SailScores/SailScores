/// <binding BeforeBuild='prebuild' Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cleanCSS = require('gulp-clean-css'),
    uglify = require('gulp-uglify-es').default,
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
paths.scripts = ['scripts/**/*.js'];

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

gulp.task("min:js", function (done) {
    gulp.src([paths.js, "!" + paths.minJs], { base: "." })
        .pipe(concat(paths.concatJsDest))
        .pipe(uglify())
        .pipe(gulp.dest("."));
    done();
});

gulp.task("min:css", function (done) {
    gulp.src([paths.css, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cleanCSS({ compatibility: 'ie8' }))
        .pipe(gulp.dest("."));
    done();
});

gulp.task("min", gulp.parallel("min:js", "min:css"));


gulp.task("copyJs", function (done) {
    gulp.src(paths.scripts).pipe(gulp.dest("wwwroot/js"));
    done();
});

gulp.task("sass", function (done) {
    gulp.src('custom.scss')
        .pipe(sass())
        .pipe(gulp.dest('wwwroot/css'));
    done();
});

gulp.task('prebuild',
    gulp.series(
        gulp.parallel("copyJs", "sass"),
        "min"),
    function (done) {
        done();
});

gulp.task('default',
    gulp.series('prebuild'),
    function (done) {
        done();
});