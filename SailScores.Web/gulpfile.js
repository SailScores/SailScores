/// <binding BeforeBuild='prebuild' Clean='clean' />
"use strict";

var gulp = require("gulp"),
    del = require("del"),
    concat = require("gulp-concat"),
    cleanCSS = require('gulp-clean-css'),
    sass = require('gulp-sass')(require('sass')),
    named = require('vinyl-named'),
    rename = require('gulp-rename'),
    webpack = require('webpack-stream'),
    exec = require('child_process').exec;

var paths = {
    webroot: "./wwwroot/"
};

paths.js = paths.webroot + "js/**/*.js";
paths.minJs = paths.webroot + "js/**/*.min.js";
paths.css = paths.webroot + "css/**/*.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatJsDest = paths.webroot + "js/**/*";
paths.concatCssDest = paths.webroot + "css/site.min.css";
paths.jsDir = paths.webroot + "js/";
paths.scriptsSrc = "Scripts/**/*.js";

gulp.task("clean:js", function () {
    return del(paths.concatJsDest);
});

gulp.task("clean:css", function () {
    return del(paths.concatCssDest);
});

gulp.task("clean", gulp.parallel("clean:js", "clean:css"));

gulp.task("copy:scripts", function () {
    return gulp.src(paths.scriptsSrc, { allowEmpty: true })
        .pipe(gulp.dest(paths.jsDir));
});

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
    // Concatenate CSS with `custom.css` emitted last so its rules (including dark-mode variables) win the cascade
    return gulp.src([
        'wwwroot/css/bootstrap.min.css',            // bootstrap first
        'wwwroot/css/*.css',                        // other css files
        '!' + paths.minCss,                         // exclude previously generated min
        '!' + 'wwwroot/css/custom.css',             // exclude custom so we can add it last
        'wwwroot/css/custom.css'                    // add custom.css last
    ], { allowEmpty: true })
        .pipe(concat(paths.concatCssDest))
        .pipe(cleanCSS({ compatibility: 'ie8' }))
        .pipe(gulp.dest("."));
});

gulp.task("sass", function () {
    return gulp.src('scss/custom.scss')
        .pipe(sass())
        .pipe(gulp.dest('wwwroot/css'));
});

// Ensure copy happens before minification so project Scripts/ files are available in wwwroot/js
gulp.task('prebuild',  gulp.series(
        "sass",
        "copy:scripts",
        "min:js",
        "min:css"));

gulp.task('default', gulp.series('prebuild'));
