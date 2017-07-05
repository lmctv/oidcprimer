<?php

require_once __DIR__.'/../vendor/autoload.php';

$app = new Silex\Application();

$app->register(new Silex\Provider\TwigServiceProvider(), 
	       array(
		   'twig.path' => __DIR__.'/templates',
	       ));

$app->register(new Silex\Provider\SessionServiceProvider());

$app->register(new Silex\Provider\MonologServiceProvider(), array(
    'monolog.logfile' => __DIR__.'/../development.log',
    'monolog.level'   => Monolog\Logger::INFO,
));

$app['oidc'] = function () use ($app) {
    $string = file_get_contents('../client.json');
    $client_config = json_decode($string, true);
    $oidc = array();


    /* 
       PLEASE NOTE: client_id and client_secret are null at first, cause
       you didn't already registered your client. That is ok, since it
       will trigger dynamic registration.
       Once registered, you'll get client_id and client_secret in the
       success page. Insert them in the clients.json file in the following
       key/value pairs: 
       
       [..]
          "client_id": null,
          "client_secret": null
       [..]
        
       Failing to do that will cause the client to register again each
       time you authenticate.
       
    */

    $client_id = $client_config['client_id'];
    $client_secret = $client_config['client_secret'];
    $client_name = 'php-client-test';

    if ($client_id and $client_secret) {
        $app['session']-> set('oidcclient', array(
            'client_id' => $client_id,
            'client_secret' => $client_secret,
        ));
    }
    
    if (null === $oidcclient = $app['session']->get('oidcclient')) {
	    $oidc = new OpenIDConnectClient('https://mitreid.org/', null, null);
	    $oidc->redirectURL = $client_config['redirect_uris'][0];
	    $oidc->setResponseTypes([$client_config['response_types'][0]]);
	    $oidc->setClientName($client_name);

	    $oidc->register(); 

	    $app['session']-> set('oidcclient', array(
	    		'client_id' => $oidc->getClientID(),
		    'client_secret' => $oidc->getClientSecret()
        ));
    }
    else {
        $oidc = new OpenIDConnectClient('https://mitreid.org/',
                                        $oidcclient['client_id'],
                                        $oidcclient['client_secret']
        );
    }

    $oidc->redirectURL = $client_config['redirect_uris'][0];
    $oidc->setResponseTypes([$client_config['response_types'][0]]);
    return $oidc;   				   
};

$app->get('/', function () use ($app) {
    $app['session']->clear();
    return $app['twig']->render('index.html');
});

$app->get('/authenticate', function () use ($app) {
    $oidc = $app['oidc'];
    if ($oidc) {
	$oidc->addScope("openid");
	$oidc->addScope("profile");
	$oidc->addScope("email");
	$oidc->authenticate();
    }
    $app->abort('500', 'Something went wrong with oidc, check console/web-container logs.');
  });
  
$app->get('/code_flow_callback', function () use ($app) {
    $oidc = $app['oidc'];
    if ($oidc) {
	$code = $_GET['code'];
	$state = $_GET['state'];

	assert ($state == $app['session']);

	$oidc->authenticate();

	$userinfo = $oidc->requestUserInfo();

        $client_id = $oidc->getClientID();
        $client_secret = $oidc->getClientSecret();
        $auth_code =  $code;
        $access_token =  $oidc->getAccessToken();
	$id_token =  $oidc->getIdToken();

        return $app['twig']->render('success_page.html',array(
            'client_id' => $client_id,
            'client_secret' => $client_secret,
            'auth_code' => $auth_code,
            'access_token' => $access_token,
            'id_token_claims' => $id_token,
            'userinfo' => json_encode($userinfo, JSON_PRETTY_PRINT),
        ));
    }
    $app->abort('500', 'Something went wrong with oidc, check console/web-container logs.');
  });

$app->post('/repost_fragment', function () use ($app) {
    return $app['twig']->render('repost_fragment.html');
    });

$app['debug'] = true;

$app->run();
