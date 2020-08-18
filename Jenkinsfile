pipeline {
    agent any
    // agent {
    //     docker {
    //         image 'mcr.microsoft.com/windows/nanoserver:1909'
    //     }
    // }

    triggers {
        pollSCM 'H/5 * * * *'
    }

    environment {
        PATH = "C:\\WINDOWS\\SYSTEM32;$PATH"
    }

    stages {
        stage('Build') {
            steps {
                bat 'SET'
                bat 'Build.cmd unstable'
            }
        }
        stage('Test') {
            steps {
                echo 'Testing..'
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'
            }
        }
    }
}
