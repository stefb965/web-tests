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
	return params.QA_USE_MONO_LANE != ''
}

def enableXI ()
{
	return params.QA_USE_XI_LANE != ''
}

def enableXM ()
{
	return params.QA_USE_XM_LANE != ''
}

def enableXA ()
{
	return params.QA_USE_XA_LANE != ''
}

def build (String targets)
{
	dir ('web-tests') {
		sh "msbuild Jenkinsfile.targets /p:Configuration=$targets"
	}
}

def buildAll ()
{
	def builder = new StringBuilder ()
	if (enableMono ()) {
		builder.append ("Console:Console-AppleTls:Console.Legacy")
	}
	if (builder.size () > 0) {
		builder.append (":")
	}
	echo "TEST!"
		
		
	if (enableXI ()) {
		if (builder.size () > 0)
			builder.append (":")
		builder.append ("IOS-Debug")
	}
	if (enableXM ()) {
		if (builder.size () > 0)
			builder.append (":")
		builder.append ("Mac")
	}
	if (enableXA ()) {
		if (builder.size () > 0)
			builder.append (":")
		builder.append ("Android-Btls")
	}
	echo "TEST #2"
	def targetList = builder.ToString ()
	echo "TARGET LIST: $targetList"
	build ($targetList)
}

node ('jenkins-mac-1') {
	timestamps {
		stage ('checkout') {
			dir ('web-tests') {
				git url: 'git@github.com:xamarin/web-tests.git'
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
			buildAll ()
		}
		stage ('martin') {
			def test = ['Foo','Bar','Monkey']
			for (int i = 0; i < test.size(); i++) {
				def name = 'test ' + i
				stage (name) {
					echo 'Hello: ' + i + ' ' + test[i]
				}
			}
		}
	}
}
