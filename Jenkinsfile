pipeline {
    agent {
        docker {
            dockerfile true
            label 'docker && windows'
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
                echo 'Testing...'
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying...'
            }
        }
    }
}
