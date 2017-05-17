#I "../../packages/build/FAKE/tools"

#r "FakeLib.dll"
#load "BuildConfig.fsx"
#load "Products.fsx"

open System
open System.IO
open System.Diagnostics
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.FileHelper
open Fake.Testing.XUnit2
open Products.Products

module Versioning =

    let Version =
        let inDir = ".build/in"
        let extractVersion (fileInfo:FileInfo) =
            Regex.Replace(fileInfo.Name, "^elasticsearch\-(.*)\.zip$", "$1")
        let explicitVersion = getBuildParam "version"
        if isNullOrEmpty explicitVersion |> not then explicitVersion
        else
            match getBuildParam "release" with
            | "1" ->
                let zips = inDir
                           |> directoryInfo
                           |> filesInDirMatching "elasticsearch*.zip"
                match zips.Length with
                | 0 -> failwithf "No elasticsearch zip file found in %s" inDir
                | 1 -> extractVersion zips.[0]
                | _ -> failwithf "Expecting one elasticsearch zip file in %s but found %i" inDir zips.Length
            | _ -> Product.LastFeedVersion Product.Elasticsearch