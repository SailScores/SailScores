/// <binding BeforeBuild='prebuild' Clean='clean' />
"use strict";

var gulp = require("gulp"),
    del = require("del"),
    concat = require("gulp-concat"),
    cleanCSS = require('gulp-clean-css'),
    sass = require('gulp-sass')(require('sass')),
    named = require('vinyl-named'),
    rename = require('gulp-rename'),
    webpack = require('webpack-stream');

var paths = {
    webroot: "./wwwroot/"
};

paths.js = paths.webroot + "js/**/*.js";
paths.minJs = paths.webroot + "js/**/*.min.js";
paths.css = paths.webroot + "css/**/*.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatJsDest = paths.webroot + "js/**/*";
paths.concatCssDest = paths.webroot + "css/site.min.css";
paths.concatTsDest = paths.webroot + "scripts/**.*";
paths.jsDir = paths.webroot + "js/";
paths.scripts = ['Scripts/*.js', 'Scripts/build/*'];

gulp.task("clean:js", function () {
    return del(paths.concatJsDest);
});

gulp.task("clean:css", function () {
    return del(paths.concatCssDest);
});

gulp.task("clean:ts", function () {
    return del(paths.concatTsDest);
});

gulp.task("clean", gulp.parallel("clean:js", "clean:css", "clean:ts"));

gulp.task("min:js", function () {
    return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
        .pipe(named())
        .pipe(webpack({
            mode: "production",
            devtool: 'source-map',
            output: {
                filename: '[name].min.js'
            },
            externals: {
                "jquery": "jQuery",
                "bootstrap": "bootstrap",
                "bootstrap-select": 'window["bootstrap-select"]',
                "d3": "d3",
                "summernote": "summernote"
            }
        }))
        .pipe(gulp.dest(paths.jsDir));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cleanCSS({ compatibility: 'ie8' }))
        .pipe(gulp.dest("."));
});

gulp.task("copyJs", function () {
    return gulp.src(paths.scripts)
        .pipe(gulp.dest("wwwroot/js"));
});

gulp.task("sass", function () {
    return gulp.src('scss/custom.scss')
        .pipe(sass())
        .pipe(gulp.dest('wwwroot/css'));
});

gulp.task('prebuild',  gulp.series(
        "copyJs",
        "sass",
        "min:js",
        "min:css"));

gulp.task('default', gulp.series('prebuild'));
