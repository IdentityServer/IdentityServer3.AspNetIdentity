properties {
    $projectName = "IdentityServer3.AspNetIdentity"
    $rootDir  = Resolve-Path .\
    $buildOutputDir = "$rootDir\build"
    $distributionDir = "$rootDir\distribution"
    $reportsDir = "$buildOutputDir\reports"
    $srcDir = "$rootDir\source"
    $solutionFilePath = "$srcDir\$projectName.sln"
    
    $buildNumber = 0
    $version = "2.0.0.0"
    $preRelease = $null
}

task default -depends RunTests, CreateNuGetPackage

task Clean {
    Remove-Item $buildOutputDir -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item $distributionDir -Force -Recurse -ErrorAction SilentlyContinue
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /t:Clean }
}

task Compile -depends Clean {
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release }
}

task RunTests -depends Compile {
    $xunitRunner = "$srcDir\packages\xunit.runners.1.9.2\tools\xunit.console.clr4.exe"
    gci . -Recurse -Include *Tests.csproj, Tests.*.csproj | % {
        $project = $_.BaseName
        if(!(Test-Path $reportsDir\xUnit\$project)){
            New-Item $reportsDir\xUnit\$project -Type Directory
        }
        .$xunitRunner "$srcDir\$project\bin\Release\$project.dll" /html "$reportsDir\xUnit\$project\index.html"
    }
}

task CreatePP {
    (Get-Content $srcDir\$projectName\$projectName.cs) | Foreach-Object {
        $_ -replace 'namespace YourRootNamespace', 'namespace $rootnamespace$' `
        -replace 'using YourRootNamespace', 'using $rootnamespace$'
        } | Set-Content $buildOutputDir\$projectName.cs.pp -Encoding UTF8
}

task CreateNuGetPackage -depends CreatePP {
    $vSplit = $version.Split('.')
    if($vSplit.Length -ne 4)
    {
      throw "Version number is invalid. Must be in the form of 0.0.0.0"
    }
    $major = $vSplit[0]
    $minor = $vSplit[1]
    $patch = $vSplit[2]
    $packageVersion =  "$major.$minor.$patch"
    if($preRelease){
      $packageVersion = "$packageVersion-$preRelease" 
    }

    if ($buildNumber -ne 0){
      $packageVersion = $packageVersion + "-build" + $buildNumber.ToString().PadLeft(5,'0')
    }

    mkdir $distributionDir

    $nuspecFilePath = "$buildOutputDir\$projectName.nuspec"
    Copy-Item $srcDir\$projectName\$projectName.nuspec $nuspecFilePath

    [Xml]$fileContents = Get-Content -Path $nuspecFilePath
    $fileContents.package.metadata.version
    #$packageVersion = $fileContents.package.metadata.version + "-build" + $buildNumber.ToString().PadLeft(5,'0')
    .$srcDir\.nuget\nuget.exe pack $nuspecFilePath -o $distributionDir -version $packageVersion
    #.$srcDir\.nuget\nuget.exe pack $srcDir\$projectName\$projectName.csproj -o $distributionDir -version $packageVersion
}


