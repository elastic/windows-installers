#I "../../packages/build/FAKE.x64/tools"
#I "../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "Fsharp.Text.RegexProvider.dll"

open System
open FSharp.Text.RegexProvider
open Fake

/// Part of a branch    
type BranchPart =
| Number of int
| X

/// A branch
/// Examples: 6, 6.x, 6.6
type Branch = {
    Major: int;
    Minor: BranchPart;  
} with

override this.ToString () =
    match this.Minor with
    | Number minor -> sprintf "%i.%i" this.Major minor
    | X -> sprintf "%i.x" this.Major
    
end
   
type private VersionRegex = Regex< @"^(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>(?![0-9a-f]{8,8})\w*?))?)(?:\-(?<BuildId>[0-9a-f]{8,8}))?$", noMethodPrefix=true >  
let private versionRegex = new VersionRegex()

/// A version
/// Examples: 6.6.0, 6.6.0-alpha1, 6.6.0-SNAPSHOT, 6.6.0-aab6bcd8
[<CustomEquality; CustomComparison>]
type Version =
    { FullVersion: string
      Major: int
      Minor: int
      Patch: int
      Prerelease: string
      BuildId: string }

    /// Compare versions. Does not include comparing BuildId
    interface IComparable<Version> with
        member this.CompareTo version =
            match this.Major.CompareTo version.Major with
            | 0 ->
                match this.Minor.CompareTo version.Minor with              
                | 0 ->
                    match this.Patch.CompareTo version.Patch with
                    | 0 ->
                        match (this.Prerelease, version.Prerelease) with
                        | ("", "") -> 0
                        | ("", prerelease) -> 1
                        | (prerelease, "") -> -1
                        | (p1, p2) -> p1.CompareTo p2
                    | n -> n
                | n -> n
            | n -> n
    
    /// Compare versions. Does not include comparing BuildId                    
    interface IComparable with   
        member this.CompareTo obj =
            match obj with
            | :? Version as v -> compare this v
            | _ -> invalidArg "obj" "cannot compare values of different types"
                  
    static member create major minor patch prerelease buildId =
        let fullVersion =
            match prerelease with
            | p when isNullOrEmpty p -> sprintf "%d.%d.%d" major minor patch
            | _ -> sprintf "%d.%d.%d-%s" major minor patch prerelease        
        { FullVersion = fullVersion
          Major = major
          Minor = minor
          Patch = patch
          Prerelease = prerelease
          BuildId = buildId }
    
    static member parse candidate =
        let m = versionRegex.Match candidate
        if m.Success = false then failwithf "Unable to parse version from: %s" candidate
        { FullVersion = m.Version.Value
          Major = m.Major.Value |> int
          Minor = m.Minor.Value |> int
          Patch = m.Patch.Value |> int
          Prerelease = m.Prerelease.Value
          BuildId = m.BuildId.Value }
        
    static member tryParse candidate =
        let m = versionRegex.Match candidate
        match m.Success with
        | true ->
            Some { FullVersion = m.Version.Value
                   Major = m.Major.Value |> int
                   Minor = m.Minor.Value |> int
                   Patch = m.Patch.Value |> int
                   Prerelease = m.Prerelease.Value
                   BuildId = m.BuildId.Value }
        | false -> None
        
    member this.IsSnapshot = this.Prerelease.EndsWith("SNAPSHOT", StringComparison.InvariantCultureIgnoreCase)
        
    override this.Equals obj =
        match obj with
        | :? Version as v ->
            this.Major = v.Major &&
            this.Minor = v.Minor &&
            this.Patch = v.Patch &&
            this.Prerelease = v.Prerelease &&
            this.BuildId = v.BuildId
        | _ -> false
        
    override this.GetHashCode () = hash (this.Major, this.Minor, this.Patch, this.Prerelease, this.BuildId)
        
    override this.ToString () =
        match this.BuildId with
        | "" -> this.FullVersion
        | _ -> sprintf "%s-%s" this.FullVersion this.BuildId
        
        
let private (|IsInt|_|) str =
    match Int32.TryParse(str) with
    | (true, int) -> Some int
    | _ -> None
 
type private BuildIdRegex = Regex< @"^(?<BuildId>[0-9a-f]{8,8})$", noMethodPrefix=true >  
let private buildIdRegex = new BuildIdRegex()
 
/// Determines if str represents a build id   
let (|IsBuildId|_|) str =
    let m = buildIdRegex.Match str
    if m.Success then Some m.Value
    else None
          
/// A requested version, based on version, branch, build id or latest
type RequestedVersion =
    | Branch of Branch
    | Version of Version
    | BuildId of string
    | Latest
    
    override this.ToString () =
        match this with
        | Branch b -> b.ToString ()
        | Version v -> v.ToString ()
        | BuildId id -> id
        | Latest -> "latest"

    static member tryParse (candidate:string) =
        let parts = candidate |> trim |> split '.'
        match parts with
        | [ IsInt major ] -> Some (Branch { Major = major; Minor = X })
        | [ IsBuildId buildId ] -> Some (BuildId buildId)
        | [ "x" ]
        | [ "X" ] -> Some Latest
        | [ IsInt major; "x" ]
        | [ IsInt major; "X" ] -> Some (Branch { Major = major; Minor = X })
        | [ IsInt major; IsInt minor ] -> Some (Branch { Major = major; Minor = Number minor; })
        | [ IsInt major; IsInt minor; IsInt patch; ] -> Some (Version (Version.create major minor patch "" ""))
        | [ IsInt major; IsInt minor; IsInt patch; prerelease; ] ->
            match prerelease |> split '-' with
            | [ ""; pre; IsBuildId buildId ] -> Some (Version (Version.create major minor patch pre buildId))
            | [ ""; IsBuildId buildId ] -> Some (Version (Version.create major minor patch "" buildId))
            | [ ""; pre; "SNAPSHOT" ]
            | [ ""; pre; "snapshot" ] -> Some (Version (Version.create major minor patch prerelease ""))
            | [ ""; pre; ] -> Some (Version (Version.create major minor patch pre ""))
            | _ -> None
        | _ -> None

/// Determines if str represents a requested version      
let (|IsRequestedVersion|_|) str = RequestedVersion.tryParse str
            
    