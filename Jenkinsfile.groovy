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

def provisionMono (String lane)
{
	provision ('Mono', lane)
}

def provisionXI (String lane)
{
	provision ('XI', lane)
}

def provisionXM (String lane)
{
	provision ('XM', lane)
}

def provisionXA (String lane)
{
	provision ('XA', lane)
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
			provisionMono (params.QA_USE_MONO_LANE)
			provisionXI (params.QA_USE_XI_LANE)
			provisionXM (params.QA_USE_XM_LANE)
			provisionXA (params.QA_USE_XA_LANE)
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
