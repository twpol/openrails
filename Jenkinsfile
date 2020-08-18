pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/windows/nanoserver:1909'
        }
    }

    triggers {
        pollSCM 'H/5 * * * *'
    }

    stages {
        stage('Build') {
            steps {
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
