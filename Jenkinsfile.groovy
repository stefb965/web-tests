#!/bin/groovy
properties([
	parameters([
		choice (name: 'QA_USE_MONO_LANE', choices: 'NONE\nmono-2017-04\nmono-2017-02\nmono-master', description: 'Mono lane'),
		choice (name: 'QA_USE_XI_LANE', choices: 'NONE\nmacios-mac-d15-2\nmacios-mac-master', description: 'XI lane'),
		choice (name: 'QA_USE_XM_LANE', choices: 'NONE\nmacios-mac-d15-2\nmacios-mac-master', description: 'XM lane'),
		choice (name: 'QA_USE_XA_LANE', choices: 'NONE\nmonodroid-mavericks-master', description: 'XA lane'),
		choice (name: 'IOS_DEVICE_TYPE', choices: 'iPhone-5s', description: ''),
		choice (name: 'IOS_RUNTIME', choices: 'iOS-10-0', description: '')
	])
])

def provision (String product, String lane)
{
	dir ('QA/Automation/XQA') {
		if ("$lane" != 'NONE') {
			sh "./build.sh --target XQASetup --category=Install$product -Verbose -- -UseLane=$lane"
		} else {
			echo "Skipping $product."
		}
	}
}

def provisionMono ()
{
	provision ('Mono', params.QA_USE_MONO_LANE)
}

def provisionXI ()
{
	provision ('XI', params.QA_USE_XI_LANE)
}

def provisionXM ()
{
	provision ('XM', params.QA_USE_XM_LANE)
}

def provisionXA ()
{
	provision ('XA', params.QA_USE_XA_LANE)
}

def enableMono ()
{
	return params.QA_USE_MONO_LANE != 'NONE'
}

def enableXI ()
{
	return params.QA_USE_XI_LANE != 'NONE'
}

def enableXM ()
{
	return params.QA_USE_XM_LANE != 'NONE'
}

def enableXA ()
{
	return params.QA_USE_XA_LANE != 'NONE'
}

def build (String configuration)
{
	sh "msbuild Jenkinsfile.targets /p:Configuration=$configuration"
}

def buildAll ()
{
	def targets = [ ]
	if (enableMono ()) {
		targets << "Console"
		targets << "Console-AppleTls"
		targets << "Console-Legacy"
	}
	if (enableXI ()) {
		targets << "IOS-Debug"
	}
	if (enableXM ()) {
		targets << "Mac"
	}
	if (enableXA ()) {
		targets << "Android-Btls"
	}
	
	if (targets.size() == 0) {
		echo "No configurations enabled!"
		currentBuild.result = "NOT_BUILT"
		return
	}
	
	def targetList = targets.join (":")
	echo "TARGET LIST: $targetList"
	build (targetList)
}

def run (String configuration, String testCategory, String resultOutput, String junitResultOutput)
{
	sh "msbuild Jenkinsfile.targets /t:RunConsole /p:Configuration=$configuration,TestCategory=$testCategory,ResultOutput=$resultOutput,JUnitResultOutput=$junitResultOutput"
}

def runTests (String category)
{
	def outputDir = pwd() + "/out/" + category
	sh "mkdir -p $outputDir"
	def resultOutput = "$outputDir/TestResult-Console-${category}.xml"
	def junitResultOutput = "$outputDir/JUnitTestResult-${category}.xml"
	echo "TEST: $resultOutput"
	run ("Debug", category, resultOutput, junitResultOutput)
//	junit keepLongStdio: true, testResults: "out/$category/*.xml"
	archiveArtifacts artifacts: "out/$category/*.xml", fingerprint: true
}

node ('jenkins-mac-1') {
	timestamps {
		stage ('checkout') {
			dir ('web-tests') {
				git url: 'git@github.com:xamarin/web-tests.git', branch: 'jenkins-pipeline'
				sh 'git clean -xffd'
			}
			dir ('QA') {
				git url: 'git@github.com:xamarin/QualityAssurance.git'
			}
		}
		stage ('provision') {
			provisionMono ()
			provisionXI ()
			provisionXM ()
			provisionXA ()
		}
		stage ('build') {
			dir ('web-tests') {
				buildAll ()
			}
		}
		stage ('martin') {
			dir ('web-tests') {
				runTests ('Martin')
			}
		}
		stage ('work') {
			dir ('web-tests') {
				runTests ('Work')
			}
		}
		stage ('all') {
			dir ('web-tests') {
				runTests ('All')
			}
		}

//		stage ('Loop') {
//			def test = ['Foo','Bar','Monkey']
//			for (int i = 0; i < test.size(); i++) {
//				def name = 'test ' + i
//				stage (name) {
//					echo 'Hello: ' + i + ' ' + test[i]
//				}
//			}
//		}
		stage ('result') {
			junit keepLongStdio: true, testResults: "out/**/*.xml"
//			archiveArtifacts artifacts: "out/**/*.xml", fingerprint: true
		}
	}
}
